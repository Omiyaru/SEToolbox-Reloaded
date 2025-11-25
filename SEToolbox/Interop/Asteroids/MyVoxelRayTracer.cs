
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using SEToolbox.Support;
using VRageMath;

namespace SEToolbox.Interop.Asteroids
{
    public static class MyVoxelRayTracer
    {
        enum MeshFace : byte
        {
            Undefined,
            Nearside,
            Farside
        }

        static readonly object Locker = new();

        public static MyVoxelMapBase GenerateVoxelMapFromModel(Model model, in Matrix3D rotationMatrix, TraceType traceType, TraceCount traceCount, TraceDirection traceDirection,
            Action<double, double> resetProgress, Action incrementProgress, Action complete, CancellationToken cancellationToken)
        {
            byte[] materials = new byte[model.MeshCount];
            byte[] faceMaterials = new byte[model.MeshCount];

            for (int i = 0; i < model.Meshes.Length; i++)
            {
                ModelMesh mesh = model.Meshes[i];
                materials[i] = mesh.MaterialIndex ?? SpaceEngineersConsts.EmptyVoxelMaterial;
                faceMaterials[i] = mesh.FaceMaterialIndex ?? SpaceEngineersConsts.EmptyVoxelMaterial;
            }

            Rect3D tbounds = model.Bounds;

            // Add 2 to either side, to allow for material padding to expose internal materials.
            const int bufferSize = 2;
            int xMin = (int)Math.Floor(tbounds.X) - bufferSize;
            int yMin = (int)Math.Floor(tbounds.Y) - bufferSize;
            int zMin = (int)Math.Floor(tbounds.Z) - bufferSize;

            int xMax = (int)Math.Ceiling(tbounds.X + tbounds.SizeX) + bufferSize;
            int yMax = (int)Math.Ceiling(tbounds.Y + tbounds.SizeY) + bufferSize;
            int zMax = (int)Math.Ceiling(tbounds.Z + tbounds.SizeZ) + bufferSize;

            Vector3I min = new(xMin, yMin, zMin);
            Vector3I max = new(xMax, yMax, zMax);

            // Do not round up the array size, as this really isn't required, and it increases the calculation time.
            int xCount = xMax - xMin;
            int yCount = yMax - yMin;
            int zCount = zMax - zMin;

            SConsole.WriteLine($"Approximate Size: {(double)(Math.Ceiling(tbounds.X + tbounds.SizeX) - Math.Floor(tbounds.X))}x{(double)(Math.Ceiling(tbounds.Y + tbounds.SizeY) - Math.Floor(tbounds.Y))}x{(double)(Math.Ceiling(tbounds.Z + tbounds.SizeZ) - Math.Floor(tbounds.Z))}");
            SConsole.WriteLine($"Bounds Size: {xCount}x{yCount}x{zCount}");

            byte[,,] finalCubic = new byte[xCount, yCount, zCount];
            byte[,,] finalMats = new byte[xCount, yCount, zCount];

            if (resetProgress != null)
            {
                long rays = 0;

                rays += ((traceDirection & TraceDirection.X) == TraceDirection.X ? yCount * zCount : 0) +
                        ((traceDirection & TraceDirection.Y) == TraceDirection.Y ? xCount * zCount : 0) +
                        ((traceDirection & TraceDirection.Z) == TraceDirection.Z ? xCount * yCount : 0);

                resetProgress.Invoke(0, rays * model.NumGeometries);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                complete?.Invoke();
                return null;
            }
            var rotateMatrix = new MatrixD(rotationMatrix.M11, rotationMatrix.M12, rotationMatrix.M13, rotationMatrix.M14,
                                           rotationMatrix.M21, rotationMatrix.M22, rotationMatrix.M23, rotationMatrix.M24,
                                           rotationMatrix.M31, rotationMatrix.M32, rotationMatrix.M33, rotationMatrix.M34,
                                           0, 0, 0, 1);

            int traceDirectionCount = 0;

            // Basic ray trace of every individual triangle.

            // Start from the last mesh, which represents the bottom of the UI stack, and overlay each other mesh on top of it.
            for (int modelIdx = model.Meshes.Length - 1; modelIdx >= 0; modelIdx--)
            {
                SConsole.WriteLine($"Model {modelIdx}");

                byte[,,] modelCubic = new byte[xCount, yCount, zCount];
                byte[,,] modelMats = new byte[xCount, yCount, zCount];

                var mesh = model.Meshes[modelIdx];
                var geometries = mesh.Geometeries;

                byte material = materials[modelIdx];
                byte faceMat = faceMaterials[modelIdx];

                if ((traceDirection & TraceDirection.X) == TraceDirection.X)
                {
                    SConsole.WriteLine("X Rays");
                    traceDirectionCount++;

                    Vector3[] rayOffsets = [
                                new(0, 0, 0),
                                new(0, -0.5f, -0.5f),
                                new(0, 0.5f, -0.5f),
                                new(0, -0.5f, 0.5f),
                                new(0, 0.5f, 0.5f)
                        ];

                    bool result = TraceRays(geometries, rotateMatrix, traceType, rayOffsets, axis: 0, min, max,
                        modelCubic, modelMats, traceDirectionCount, material, faceMat, incrementProgress, cancellationToken);

                    if (!result)
                    {
                        complete?.Invoke();
                        return null;
                    }
                }

                if ((traceDirection & TraceDirection.Y) == TraceDirection.Y)
                {
                    SConsole.WriteLine("Y Rays");
                    traceDirectionCount++;

                    Vector3[] rayOffsets = [
                        new( 0, 0, 0),
                        new(-0.5f, 0, -0.5f),
                        new( 0.5f, 0, -0.5f),
                        new(-0.5f, 0,  0.5f),
                        new( 0.5f, 0,  0.5f)
                    ];

                    bool result = TraceRays(geometries, rotateMatrix, traceType, rayOffsets, axis: 1, min, max,
                        modelCubic, modelMats, traceDirectionCount, material, faceMat, incrementProgress, cancellationToken);

                    if (!result)
                    {
                        complete?.Invoke();
                        return null;
                    }
                }

                if ((traceDirection & TraceDirection.Z) == TraceDirection.Z)
                {
                    SConsole.WriteLine("Z Rays");
                    traceDirectionCount++;

                    Vector3[] rayOffsets = [
                        new( 0, 0, 0),
                        new(-0.5f, -0.5f, 0),
                        new( 0.5f, -0.5f, 0),
                        new(-0.5f,  0.5f, 0),
                        new( 0.5f,  0.5f, 0)
                    ];

                    bool result = TraceRays(geometries, rotateMatrix, traceType, rayOffsets, axis: 2, min, max,
                        modelCubic, modelMats, traceDirectionCount, material, faceMat, incrementProgress, cancellationToken);

                    if (!result)
                    {
                        complete?.Invoke();
                        return null;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    complete?.Invoke();
                    return null;
                }

             var range = from X in Enumerable.Range(0, xCount)
                         from Y in Enumerable.Range(0, yCount)
                         from Z in Enumerable.Range(0, zCount)
                         select new { X, Y, Z };
                Parallel.ForEach(range, item =>
                {
                    int x = item.X;
                    int y = item.Y;
                    int z = item.Z;

                    byte content = modelCubic[x, y, z];
                    byte mat = modelMats[x, y, z];

              
                    if (mat == 0xff && content != 0)
                    {
                        finalCubic[x, y, z] = (byte)Math.Max(finalCubic[x, y, z] - content, 0);
                    }
                    else if (content != 0)
                    {
                        finalCubic[x, y, z] = Math.Max(finalCubic[x, y, z], content);
                        finalMats[x, y, z] = mat;
                    }
                    else if (finalCubic[x, y, z] == 0 && finalMats[x, y, z] == 0 && mat != 0xff)
                    {
                        finalMats[x, y, z] = mat;
                    }
                });
            }

            // End models

            if (cancellationToken.IsCancellationRequested)
            {
                complete?.Invoke();
                return null;
            }
            //lookintoSurcaceMaterial

            Vector3I size = new(xCount, yCount, zCount);
            // TODO: At the moment the Mesh list is not complete, so the faceMaterial setting is kind of vague.
            byte? defaultMaterial = model.Meshes[0].MaterialIndex; // Use the FaceMaterial from the first Mesh in the object list.
            byte? faceMaterial = model.Meshes[0].FaceMaterialIndex; // Use the FaceMaterial from the first Mesh in the object list.

            if (model.Meshes.Length > 0)
            {
                defaultMaterial = model.Meshes.Min(m => m.MaterialIndex);
                faceMaterial = model.Meshes.Min(m => m.FaceMaterialIndex);
            }

            void CellAction(ref MyVoxelBuilderArgs args)
            {
                // Center the finalCubic structure within the voxel Volume.
                Vector3I pointOffset = (args.Size - size) / 2;

                // The model is only shaped according to its volume, not the voxel which is cubic.
                if (args.CoordinatePoint.X >= pointOffset.X && args.CoordinatePoint.X < pointOffset.X + xCount &&
                    args.CoordinatePoint.Y >= pointOffset.Y && args.CoordinatePoint.Y < pointOffset.Y + yCount &&
                    args.CoordinatePoint.Z >= pointOffset.Z && args.CoordinatePoint.Z < pointOffset.Z + zCount)
                {
                    var coord = args.CoordinatePoint - pointOffset;

                    args.Volume = finalCubic[coord.X, coord.Y, coord.Z];
                    args.MaterialIndex = finalMats[coord.X, coord.Y, coord.Z];
                }
            }

            var voxelMap = MyVoxelBuilder.BuildAsteroid(true, size, defaultMaterial.Value, faceMaterial, CellAction);


            complete?.Invoke();

            return voxelMap;
        }

        static bool TraceRays(MeshGeometery[] geometries, MatrixD rotateMatrix, TraceType traceType, Vector3[] rayOffsets, int axis,
            Vector3I min, Vector3I max, byte[,,] modelCubic, byte[,,] modelMats, int traceDirectionCount, byte material, byte faceMaterial,
            Action incrementProgress, CancellationToken cancellationToken)
        {
            int xCount = max.X - min.X;
            int yCount = max.Y - min.Y;
            int zCount = max.Z - min.Z;

            int iterCount = axis switch
            {
                0 => yCount * zCount,
                1 => xCount * zCount,
                2 => xCount * yCount,
                _ => 0,
            };

            ParallelLoopResult loopResult;

            try
            {
                ParallelOptions options = new() { CancellationToken = cancellationToken };
                loopResult = Parallel.For(0, iterCount, options, InitLocalState, IntersectRays, args => { });
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (AggregateException ag) when (ag.InnerExceptions.All(e => e is OperationCanceledException))
            {
                return false;
            }

            GC.Collect();

            return loopResult.IsCompleted;

            (List<RayHit>, byte, byte) InitLocalState() => (new List<RayHit>(), material, faceMaterial);

            (List<RayHit>, byte, byte) IntersectRays(int loopIndex, ParallelLoopState loopState, (List<RayHit> RayHits, byte Mat, byte FaceMat) args)
            {
                int x = 0;
                int y = 0;
                int z = 0;

                switch (axis)
                {
                    case 0:
                        y = loopIndex / zCount + min.Y;
                        z = loopIndex % zCount + min.Z;
                        break;
                    case 1:
                        x = loopIndex / zCount + min.X;
                        z = loopIndex % zCount + min.Z;
                        break;
                    case 2:
                        x = loopIndex / yCount + min.X;
                        y = loopIndex % yCount + min.Y;
                        break;
                }

                List<RayHit> rayHits = args.RayHits;

                TestRays(geometries, rotateMatrix, traceType, rayOffsets, rayHits, min, max, new Vector3I(x, y, z), axis, incrementProgress, cancellationToken);

                if (rayHits.Count > 1)
                {
                    switch (axis)
                    {
                        case 0:
                            AccumVolume<XAxisSelector>(rayHits, traceType, rayOffsets.Length, min, max, x, y, z,
                                modelCubic, modelMats, traceDirectionCount, args.Mat, args.FaceMat);
                            break;
                        case 1:
                            AccumVolume<YAxisSelector>(rayHits, traceType, rayOffsets.Length, min, max, x, y, z,
                                modelCubic, modelMats, traceDirectionCount, args.Mat, args.FaceMat);
                            break;
                        case 2:
                            AccumVolume<ZAxisSelector>(rayHits, traceType, rayOffsets.Length, min, max, x, y, z,
                                modelCubic, modelMats, traceDirectionCount, args.Mat, args.FaceMat);
                            break;
                    }
                }

                rayHits.Clear();
                return args;
            }
        }

        static void TestRays(MeshGeometery[] geometries, MatrixD rotateMatrix, TraceType traceType,
            Vector3[] rayOffsets, List<RayHit> rayHits, Vector3I min, Vector3I max, Vector3I coord, int axis,
            Action incrementProgress, CancellationToken cancellationToken)
        {
            Vector3 axisMask = axis switch
            {
                0 => new Vector3(0, 1, 1),
                1 => new Vector3(1, 0, 1),
                2 => new Vector3(1, 1, 0),
                _ => default,
            };

            Span<VRageMath.Vector3D> maskedOffsets = stackalloc VRageMath.Vector3D[rayOffsets.Length];

            for (int i = 0; i < rayOffsets.Length; i++)
            {
                // How far to check in from the proposed Volumetric edge.
                // This number is just made up, but small enough that it still represents the corner edge of the Volumetric space.
                // But still large enough that it isn't the exact corner.
                const double edgeOffset = 0.0000045f;

                Vector3 ro = rayOffsets[i];

                VRageMath.Vector3D offset = new(
                    ro.X > 0 ? -edgeOffset : edgeOffset,
                    ro.Y > 0 ? -edgeOffset : edgeOffset,
                    ro.Z > 0 ? -edgeOffset : edgeOffset);

                if (traceType == TraceType.Even)
                    offset += 0.5f;

                maskedOffsets[i] = offset * axisMask;
            }

            Vector3 axisMin = min * (Vector3.One - axisMask);
            Vector3 axisMax = max * (Vector3.One - axisMask);
            Vector3 coordF = (Vector3)coord;
            Vector3 coordMin = axisMin + coordF;
            Vector3 coordMax = axisMax + coordF;
            var rayPoints = new Point3DCollection();

            foreach (MeshGeometery geometry in geometries)
            {
                for (int t = 0; t < geometry.Triangles.Length; t += 3)
                {
                    rayPoints[0] = geometry.Positions[geometry.Triangles[t]];
                    rayPoints[1] = geometry.Positions[geometry.Triangles[t + 1]];
                    rayPoints[2] = geometry.Positions[geometry.Triangles[t + 2]];

                    rayPoints[0] = VRageMath.Vector3D.TransformNormal(rayPoints[0].ToVector3D(), rotateMatrix).ToPoint3D();
                    rayPoints[1] = VRageMath.Vector3D.TransformNormal(rayPoints[1].ToVector3D(), rotateMatrix).ToPoint3D();
                    rayPoints[2] = VRageMath.Vector3D.TransformNormal(rayPoints[2].ToVector3D(), rotateMatrix).ToPoint3D();

                    for (int i = 0; i < rayOffsets.Length; i++)
                    {
                        Vector3 ro = rayOffsets[i];
                        VRageMath.Vector3D o = maskedOffsets[i];
                        VRageMath.Vector3D start = coordMin + ro + o;

                        if (axis != 0)
                        {
                            if ((rayPoints[0].X < start.X && rayPoints[1].X < start.X && rayPoints[2].X < start.X) ||
                                (rayPoints[0].X > start.X && rayPoints[1].X > start.X && rayPoints[2].X > start.X))
                                continue;
                        }

                        if (axis != 1)
                        {
                            if ((rayPoints[0].Y < start.Y && rayPoints[1].Y < start.Y && rayPoints[2].Y < start.Y) ||
                                (rayPoints[0].Y > start.Y && rayPoints[1].Y > start.Y && rayPoints[2].Y > start.Y))
                                continue;
                        }

                        if (axis != 2)
                        {
                            if ((rayPoints[0].Z < start.Z && rayPoints[1].Z < start.Z && rayPoints[2].Z < start.Z) ||
                                (rayPoints[0].Z > start.Z && rayPoints[1].Z > start.Z && rayPoints[2].Z > start.Z))
                                continue;
                        }

                        VRageMath.Vector3D end = (coordMax + ro) + o;



                        if (MeshHelper.RayIntersectTriangleRound(rayPoints, start.ToPoint3D(), end.ToPoint3D(), out Point3D intersect, out int normal))
                            rayHits.Add(new RayHit(intersect, normal, i));
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                if (incrementProgress != null)
                {
                    lock (Locker)
                        incrementProgress.Invoke();
                }
            }
        }

        interface IAxisValueSelector
        {
            int Axis { get; }

            double GetValue(Point3D point);

            void UpdateRelCoord(ref int x, ref int y, ref int z, int c, int axisMin);
        }

        const MethodImplOptions Inline = MethodImplOptions.AggressiveInlining;

        readonly struct XAxisSelector : IAxisValueSelector
        {
            public int Axis { [MethodImpl(Inline)] get => 0; }

            [MethodImpl(Inline)]
            public double GetValue(Point3D point) => point.X;

            [MethodImpl(Inline)]
            public void UpdateRelCoord(ref int x, ref int y, ref int z, int c, int axisMin) { x = c - axisMin; }
        }

        readonly struct YAxisSelector : IAxisValueSelector
        {
            public int Axis { [MethodImpl(Inline)] get => 1; }

            [MethodImpl(Inline)]
            public double GetValue(Point3D point) => point.Y;

            [MethodImpl(Inline)]
            public void UpdateRelCoord(ref int x, ref int y, ref int z, int c, int axisMin) { y = c - axisMin; }
        }

        readonly struct ZAxisSelector : IAxisValueSelector
        {
            public int Axis { [MethodImpl(Inline)] get => 2; }

            [MethodImpl(Inline)]
            public double GetValue(Point3D point) => point.Z;

            [MethodImpl(Inline)]
            public void UpdateRelCoord(ref int x, ref int y, ref int z, int c, int axisMin) { z = c - axisMin; }
        }

        static void AccumVolume<TSelector>(List<RayHit> rayHits, TraceType traceType, int testRayCount,
            Vector3I min, Vector3I max, int x, int y, int z, byte[,,] modelCubic, byte[,,] modelMats,
            int traceDirectionCount, byte material, byte faceMaterial)
            where TSelector : struct, IAxisValueSelector
        {
            (double Point, MeshFace Face, int TestRayIndex)[] orderedHits = [.. rayHits.Select(t => (Point: default(TSelector).GetValue(t.Point), t.Face, t.TestRayIndex))
                                     .Distinct().OrderBy(k => k.Point)];

            TSelector selector = default;

            int startCoord, endCoord;

            float startOffset = 0.5f;
            float endOffset = 0.5f;
            float volumeOffset = 0.5f;

            if (traceType == TraceType.Odd)
            {
                startCoord = (int)Math.Round(orderedHits[0].Point);
                endCoord = (int)Math.Round(orderedHits[orderedHits.Length - 1].Point);

                startOffset = 0.5f;
                endOffset = 0.5f;
                volumeOffset = 0.5f;
            }
            else// if (traceType == TraceType.Even)
            {
                startCoord = (int)Math.Floor(orderedHits[0].Point);
                endCoord = (int)Math.Floor(orderedHits[orderedHits.Length - 1].Point);

                startOffset = 0.0f;
                endOffset = 1.0f;
                volumeOffset = 1.0f;
            }

            int xRel = x - min.X;
            int yRel = y - min.Y;
            int zRel = z - min.Z;

            int axisMin = 0, axisMax = 0;

            axisMin = selector.Axis switch
            {
                0 => min.X,
                1 => min.Y,
                2 => min.Z,
                _ => throw new ArgumentOutOfRangeException()
            };

            axisMax = selector.Axis switch
            {
                0 => max.X,
                1 => max.Y,
                2 => max.Z,
                _ => throw new ArgumentOutOfRangeException()
            };

            double hitWeight = 1.0 / testRayCount;
            Span<int> surfaceCounts = stackalloc int[testRayCount];

            for (int c = startCoord; c <= endCoord; c++)
            {
                double volume = 0;

                for (int i = 0; i < surfaceCounts.Length; i++)
                    volume += (surfaceCounts[i] > 0 ? 1 : 0) * hitWeight;

                foreach ((double Point, MeshFace Face, int TestRayIndex) hit in orderedHits)
                {
                    double p = hit.Point;

                    if (p < c - startOffset || p >= c + endOffset) // Check if the point in this cell
                        continue;

                    double v = Math.Max(0, c + volumeOffset - p) * hitWeight;

                    if (hit.Face == MeshFace.Farside)
                    {
                        if (surfaceCounts[hit.TestRayIndex]++ == 0)
                            volume += v;
                    }
                    else if (hit.Face == MeshFace.Nearside)
                    {
                        int sc = surfaceCounts[hit.TestRayIndex] - 1;

                        if (sc == 0)
                            volume -= v;

                        surfaceCounts[hit.TestRayIndex] = Math.Max(0, sc);
                    }
                }

                selector.UpdateRelCoord(ref xRel, ref yRel, ref zRel, c, axisMin);

                if (traceDirectionCount > 1)
                {
                    // Average with the pre-existing volume.
                    double preVolumme = modelCubic[xRel, yRel, zRel] / 255.0;
                    volume = (preVolumme * ((traceDirectionCount - 1) / (double)traceDirectionCount)) + (volume / (double)traceDirectionCount);
                }

                modelCubic[xRel, yRel, zRel] = (byte)Math.Round(volume * 255);
                modelMats[xRel, yRel, zRel] = material;
            }

            if (faceMaterial == 0xff)
                return;

            for (int i = 1; i < 6; i++)
            {
                int c = startCoord - i;

                if (c > axisMin)
                {
                    selector.UpdateRelCoord(ref xRel, ref yRel, ref zRel, c, axisMin);

                    if (modelCubic[xRel, yRel, zRel] == 0)
                        modelMats[xRel, yRel, zRel] = faceMaterial;
                }

                c = endCoord + i;

                if (c < axisMax)
                {
                    selector.UpdateRelCoord(ref xRel, ref yRel, ref zRel, c, axisMin);

                    if (modelCubic[xRel, yRel, zRel] == 0)
                        modelMats[xRel, yRel, zRel] = faceMaterial;
                }
            }
        }

        public class Model
        {
            public Rect3D Bounds;
            public long NumGeometries;
            public ModelMesh[] Meshes;

            public int MeshCount => Meshes.Length;

            public Model(Model3DGroup model, Size3D scale, Matrix3D rotateMatrix, byte material)
            {
                if (scale.X > 0 && scale.Y > 0 && scale.Z > 0
                    && scale.X != 1.0 && scale.Y != 1.0 && scale.Z != 1.0)
                {
                    model.TransformScale(scale.X, scale.Y, scale.Z);
                }

                Rect3D bounds = model.Bounds;

                // Attempt to offset the model, so it's only caulated from zero (0) and up, instead of using zero (0) as origin.
                model.Transform = new TranslateTransform3D(-bounds.X, -bounds.Y, -bounds.Z);

                if (!rotateMatrix.IsIdentity)
                    bounds = new MatrixTransform3D(rotateMatrix).TransformBounds(bounds);

                Bounds = bounds;

                model.Transform = new TranslateTransform3D(-bounds.X, -bounds.Y, -bounds.Z);

                List<MeshGeometery> geometries = [];

                foreach (Model3D c in model.Children)
                {
                    GeometryModel3D gm = (GeometryModel3D)c;

                    if (gm.Geometry is MeshGeometry3D g)
                        geometries.Add(new MeshGeometery(g));
                }

                // TODO: If this is only ever used with one mesh then remove the MeshGeometry concept
                Meshes = [new([.. geometries], material, material)];
                NumGeometries = geometries.Count;
            }
        }

        public class ModelMesh(MeshGeometery[] geometeries, byte? materialIndex, byte? faceMaterialIndex)
        {
            public MeshGeometery[] Geometeries { get; set; } = geometeries;
            public byte? MaterialIndex { get; set; } = materialIndex;
            public byte? FaceMaterialIndex { get; set; } = faceMaterialIndex;
        }

        public class MeshGeometery(int[] triangles, Point3D[] positions)
        {
            public int[] Triangles { get; set; } = triangles;
            public Point3D[] Positions { get; set; } = positions;

            public MeshGeometery(MeshGeometry3D meshGeometry)
                : this([.. meshGeometry.TriangleIndices], [.. meshGeometry.Positions]) { }
        }

        struct RayHit : IEquatable<RayHit>
        {
            public Point3D Point;
            public MeshFace Face;
            public int TestRayIndex;

            public RayHit(Point3D point, int normal, int testRayIndex)
            {
                Point = point;
                Face = MeshFace.Undefined;

                if (normal == 1)
                    Face = MeshFace.Nearside;
                else if (normal == -1)
                    Face = MeshFace.Farside;

                TestRayIndex = testRayIndex;
            }

            public readonly bool Equals(RayHit other)
            {
                return Point == other.Point && Face == other.Face && TestRayIndex == other.TestRayIndex;
            }
        }
    }
}
