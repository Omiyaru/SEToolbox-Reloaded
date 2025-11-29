using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using SEToolbox.Interop;
using VRage.Game;
using VRageMath;
using Direction = VRageMath.Base6Directions.Direction;
using System.IO;


namespace SEToolbox.Support
{
    public static class Modelling
    {
        #region PreviewReadVolumetricModel


        public static Rect3D PreviewVolumetricModel(string modelFile, out Model3D model)
        {
            if (modelFile != null)
            {
                try
                {
                    model = MeshHelper.Load(modelFile, ignoreErrors: true);
                }
                catch
                {
                    model = null;
                    return Rect3D.Empty;
                }

                if (model.Bounds == Rect3D.Empty)
                {
                    model = null;
                    return Rect3D.Empty;
                }

                return model.Bounds;
            }

            model = null;
            return Rect3D.Empty;
        }

        #endregion

        #region ReadVolumetricModel

        public static CubeType[][][] ReadVolumetricModel(string modelFile, double scaleMultiplyier, Transform3D transform, ModelTraceVoxel traceType)
        {
            return ReadVolumetricModel(modelFile, scaleMultiplyier, scaleMultiplyier, scaleMultiplyier, transform, traceType, null, null);
        }

        public static CubeType[][][] ReadVolumetricModel(string modelFile, double scaleMultiplyier, Transform3D transform, ModelTraceVoxel traceType, Action<double, double> resetProgress, Action incrementProgress)
        {
            return ReadVolumetricModel(modelFile, scaleMultiplyier, scaleMultiplyier, scaleMultiplyier, transform, traceType, resetProgress, incrementProgress);
        }

        /// <summary>
        /// Volumes are calculated across axis where they are whole numbers (rounded to 0 decimal places).
        /// </summary>
        /// <param name="modelFile"></param>
        /// <param name="scaleMultiplyierX"></param>
        /// <param name="scaleMultiplyierY"></param>
        /// <param name="scaleMultiplyierZ"></param>
        /// <param name="transform"></param>
        /// <param name="traceType"></param>
        /// <param name="resetProgress"></param>
        /// <param name="incrementProgress"></param>
        /// <returns></returns>
        public static CubeType[][][] ReadVolumetricModel(string modelFile, double scaleMultiplyierX, double scaleMultiplyierY, double scaleMultiplyierZ, Transform3D transform, ModelTraceVoxel traceType, Action<double, double> resetProgress, Action incrementProgress)
        {
            if (string.IsNullOrWhiteSpace(modelFile))
            {
                throw new ArgumentException("Model file is empty.", nameof(modelFile));
            }
            // How far to check in from the proposed Volumetric edge.
            // This number is just made up, but small enough that it still represents the corner edge of the Volumetric space.
            // But still large enough that it isn't the exact corner.
            Model3DGroup model = MeshHelper.Load(modelFile, ignoreErrors: true) ?? throw new FileNotFoundException("Model file not found.", nameof(modelFile));
            if (model.Bounds == Rect3D.Empty)
            {
                throw new InvalidOperationException("Model bounds are empty.");
            }

            const double offset = 0.00000456f;

            if (scaleMultiplyierX <= 0 || scaleMultiplyierY <= 0 || scaleMultiplyierZ <= 0)
            {
                throw new ArgumentException("Scale multiplyier must be greater than zero.", nameof(scaleMultiplyierX));
            }

            if (scaleMultiplyierX != 1.0f || scaleMultiplyierY != 1.0f || scaleMultiplyierZ != 1.0f)
            {
                model.TransformScale(scaleMultiplyierX, scaleMultiplyierY, scaleMultiplyierZ);
            }

            Rect3D tbounds = model.Bounds;

            tbounds = transform.TransformBounds(tbounds);


            int xMin = (int)Math.Floor(tbounds.X);
            int yMin = (int)Math.Floor(tbounds.Y);
            int zMin = (int)Math.Floor(tbounds.Z);

            int xMax = (int)Math.Ceiling(tbounds.X + tbounds.SizeX);
            int yMax = (int)Math.Ceiling(tbounds.Y + tbounds.SizeY);
            int zMax = (int)Math.Ceiling(tbounds.Z + tbounds.SizeZ);

            int xCount = xMax - xMin;
            int yCount = yMax - yMin;
            int zCount = zMax - zMin;

            CubeType[][][] cubic = ArrayHelper.Create<CubeType>(xCount, yCount, zCount);

            if (resetProgress != null)
            {
                double count = (from GeometryModel3D gm in model.Children select gm.Geometry as MeshGeometry3D).Aggregate<MeshGeometry3D, double>(0, (current, g) => current + (g.TriangleIndices.Count / 3));
                if (traceType == ModelTraceVoxel.ThinSmoothed || traceType == ModelTraceVoxel.ThickSmoothedUp)
                {
                    count += xCount * yCount * zCount * 3;
                }

                resetProgress.Invoke(0, count);
            }

            #region basic ray trace of every individual triangle.

            foreach (Model3D model3D in model.Children)
            {
                GeometryModel3D gm = (GeometryModel3D)model3D;
                MeshGeometry3D g = gm.Geometry as MeshGeometry3D;
                System.Windows.Media.Color color = Colors.Transparent;

                if (gm.Material is MaterialGroup materials)
                {
                    DiffuseMaterial material = materials.Children.OfType<DiffuseMaterial>().FirstOrDefault();

                    if (material != null && material.Brush is SolidColorBrush brush)
                    {
                        color = brush.Color;
                    }
                }

                for (int t = 0; t < g.TriangleIndices.Count; t += 3)
                {
                    incrementProgress?.Invoke();

                    Point3D p1 = g.Positions[g.TriangleIndices[t]];
                    Point3D p2 = g.Positions[g.TriangleIndices[t + 1]];
                    Point3D p3 = g.Positions[g.TriangleIndices[t + 2]];

                    if (transform != null)
                    {
                        p1 = transform.Transform(p1);
                        p2 = transform.Transform(p2);
                        p3 = transform.Transform(p3);
                    }

                    Point3D minBound = MeshHelper.Min(p1, p2, p3).Floor();
                    Point3D maxBound = MeshHelper.Max(p1, p2, p3).Ceiling();
                    Point3DCollection rayPoints = [];
                    List<Point3D> rays = [];

                    for (double y = minBound.Y; y < maxBound.Y; y++)
                    {
                        for (double z = minBound.Z; z < maxBound.Z; z++)
                        {
                            if (traceType == ModelTraceVoxel.Thin || traceType == ModelTraceVoxel.ThinSmoothed)
                            {
                                rays.Clear();

                                rays.Add(new(xMin, y + 0.5 + offset, z + 0.5 + offset));
                                rays.Add(new(xMax, y + 0.5 + offset, z + 0.5 + offset));
                            }
                            else
                            {
                                rays.Clear();

                                rays.Add(new(xMin, y + offset, z + offset));
                                rays.Add(new(xMax, y + offset, z + offset));
                                rays.Add(new(xMin, y + 1 - offset, z + offset));
                                rays.Add(new(xMax, y + 1 - offset, z + offset));
                                rays.Add(new(xMin, y + offset, z + 1 - offset));
                                rays.Add(new(xMax, y + offset, z + 1 - offset));
                                rays.Add(new(xMin, y + 1 - offset, z + 1 - offset));
                                rays.Add(new(xMax, y + 1 - offset, z + 1 - offset));
                            }

                            try
                            {
                                if (MeshHelper.RayIntersectTriangleRound(rayPoints, rays, out Point3D intersect, out int normal))
                                {
                                    cubic[(int)Math.Floor(intersect.X) - xMin][(int)Math.Floor(intersect.Y) - yMin][(int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                                }
                            }
                            catch (Exception e)
                            {
                                SConsole.WriteLine(e.Message);
                            }
                        }
                    }

                    for (double x = minBound.X; x < maxBound.X; x++)
                    {
                        for (double z = minBound.Z; z < maxBound.Z; z++)
                        {
                            if (traceType == ModelTraceVoxel.Thin || traceType == ModelTraceVoxel.ThinSmoothed)
                            {
                                rays.Clear();

                                rays.Add(new(x + 0.5 + offset, yMin, z + 0.5 + offset));
                                rays.Add(new(x + 0.5 + offset, yMax, z + 0.5 + offset));
                            }
                            else
                            {
                                rays.Clear();

                                rays.Add(new(x + offset, yMin, z + offset));
                                rays.Add(new(x + offset, yMax, z + offset));
                                rays.Add(new(x + 1 - offset, yMin, z + offset));
                                rays.Add(new(x + 1 - offset, yMax, z + offset));
                                rays.Add(new(x + offset, yMin, z + 1 - offset));
                                rays.Add(new(x + offset, yMax, z + 1 - offset));
                                rays.Add(new(x + 1 - offset, yMin, z + 1 - offset));
                                rays.Add(new(x + 1 - offset, yMax, z + 1 - offset));
                            }

                            try
                            {
                                if (MeshHelper.RayIntersectTriangleRound(rayPoints, rays, out Point3D intersect, out int normal))
                                {
                                    cubic[(int)Math.Floor(intersect.X) - xMin][(int)Math.Floor(intersect.Y) - yMin][(int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                                }
                            }
                            catch (Exception e)
                            {
                                SConsole.WriteLine(e.Message);
                            }
                        }
                    }

                    for (double x = minBound.X; x < maxBound.X; x++)
                    {
                        for (double y = minBound.Y; y < maxBound.Y; y++)
                        {
                            if (traceType == ModelTraceVoxel.Thin || traceType == ModelTraceVoxel.ThinSmoothed)
                            {
                                rays.Clear();

                                rays.Add(new(x + 0.5 + offset, y + 0.5 + offset, zMin));
                                rays.Add(new(x + 0.5 + offset, y + 0.5 + offset, zMax));
                            }
                            else
                            {
                                rays.Clear();

                                rays.Add(new(x + offset, y + offset, zMin));
                                rays.Add(new(x + offset, y + offset, zMax));
                                rays.Add(new(x + 1 - offset, y + offset, zMin));
                                rays.Add(new(x + 1 - offset, y + offset, zMax));
                                rays.Add(new(x + offset, y + 1 - offset, zMin));
                                rays.Add(new(x + offset, y + 1 - offset, zMax));
                                rays.Add(new(x + 1 - offset, y + 1 - offset, zMin));
                            }

                            if (MeshHelper.RayIntersectTriangleRound(rayPoints, rays, out Point3D intersect, out int normal))
                            {
                                cubic[(int)Math.Floor(intersect.X) - xMin][(int)Math.Floor(intersect.Y) - yMin][(int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                            }
                        }
                    }
                }
            }

            #endregion

            CrawlExterior(cubic);

            if (traceType == ModelTraceVoxel.ThinSmoothed || traceType == ModelTraceVoxel.ThickSmoothedUp)
            {
                CalculateAddedInverseCorners(cubic, incrementProgress);//, incrementProgress)?
                CalculateAddedSlopes(cubic, incrementProgress);
                CalculateAddedCorners(cubic, incrementProgress);
            }

            // Uncomment the following block to execute the calculations
            if (traceType == ModelTraceVoxel.ThickSmoothedDown)
            {
                CalculateSubtractedCorners(cubic, incrementProgress);
                CalculateSubtractedSlopes(cubic, incrementProgress);
                CalculateSubtractedInverseCorners(cubic, incrementProgress);
            }

            return cubic;
        }

        #endregion

        #region ReadVolumetricModelAlt

        // WIP.
        public static CubeType[][][] ReadVolumetricModelAlt(string modelFile)
        {
            Model3DGroup model = MeshHelper.Load(modelFile, ignoreErrors: true);

            // Calculate bounds and voxel grid size
            Rect3D bounds = model.Bounds;
            int xMin = (int)Math.Floor(bounds.Location.X);
            int yMin = (int)Math.Floor(bounds.Y);
            int zMin = (int)Math.Floor(bounds.Z);
            int xMax = (int)Math.Ceiling(bounds.X + bounds.SizeX);
            int yMax = (int)Math.Ceiling(bounds.Y + bounds.SizeY);
            int zMax = (int)Math.Ceiling(bounds.Z + bounds.SizeZ);

            int xCount = xMax - xMin;
            int yCount = yMax - yMin;
            int zCount = zMax - zMin;

            var cubic = ArrayHelper.Create<CubeType>(xCount, yCount, zCount);

            Dictionary<Point3D, byte[]> blockDict = [];

            #region basic ray trace of every individual triangle.

            foreach (GeometryModel3D gm in model.Children.Cast<GeometryModel3D>())
            {
                MeshGeometry3D g = gm.Geometry as MeshGeometry3D;
                Point3DCollection rayPoints = [];
                for (int t = 0; t < g.TriangleIndices.Count; t += 3)
                {
                    rayPoints[0] = g.Positions[g.TriangleIndices[t]];
                    rayPoints[1] = g.Positions[g.TriangleIndices[t + 1]];
                    rayPoints[2] = g.Positions[g.TriangleIndices[t + 2]];

                    Point3D minBound = MeshHelper.Min(rayPoints[0], rayPoints[1], rayPoints[2]).Floor();
                    Point3D maxBound = MeshHelper.Max(rayPoints[0], rayPoints[1], rayPoints[2]).Ceiling();

                    for (double y = minBound.Y; y < maxBound.Y; y++)
                    {
                        for (double z = minBound.Z; z < maxBound.Z; z++)
                        {
                            Point3D roundPointA = new(xMin, y, z);
                            Point3D roundPointB = new(xMax, y, z);

                            if (MeshHelper.RayIntersectTriangleRound(rayPoints, roundPointA, roundPointB, out Point3D intersect, out _)) // Ray
                            {
                                double CrossY = intersect.Y; double CrossX = intersect.X; double CrossZ = intersect.Z;
                                double RoundX = Math.Round(CrossX); double RoundY = Math.Round(CrossY); double RoundZ = Math.Round(CrossZ);

                                Point3D blockPoint = intersect.Round();
                                _ = new byte[8];

                                if (!blockDict.ContainsKey(blockPoint))
                                    blockDict[blockPoint] = new byte[8];

                                double diffX = RoundX - CrossX;
                                double diffY = RoundY - CrossY;
                                double diffZ = RoundZ - CrossZ;

                                byte[] cornerHit = new byte[8];

                                int index = (diffX > 0 ? 4 : 0) | (diffY > 0 ? 2 : 0) | (diffZ > 0 ? 1 : 0);
                                cornerHit[index] = 1;
                                // Merge cornerHit into blockDict[blockPoint]
                                for (int i = 0; i < 8; i++)
                                {
                                    blockDict[blockPoint][i] = (byte)Math.Min(1, blockDict[blockPoint][i] + cornerHit[i]);
                                }
                                int crossFloorX = (int)Math.Floor(CrossX);
                                int crossFloorY = (int)Math.Floor(CrossY);
                                int crossFloorZ = (int)Math.Floor(CrossZ);
                                cubic[crossFloorX - xMin][crossFloorY - yMin][crossFloorZ - zMin] = CubeType.Cube;
                            }
                        }
                    }

                    for (double y = minBound.Y; y < maxBound.Y; y++)
                    {
                        for (double z = minBound.Z; z < maxBound.Z; z++)
                        {
                            Point3D roundPointA = new(xMin, y, z);
                            Point3D roundPointB = new(xMax, y, z);
                            if (MeshHelper.RayIntersectTriangle(rayPoints, roundPointA, roundPointB, out Point3D localIntersect, out _)) // Ray
                            {
                                int LocFloorCrossX = (int)Math.Floor(localIntersect.X);
                                int LocFloorCrossY = (int)Math.Floor(localIntersect.Y);
                                int LocFloorCrossZ = (int)Math.Floor(localIntersect.Z);
                                cubic[LocFloorCrossX - xMin][LocFloorCrossY - yMin][LocFloorCrossZ - zMin] = CubeType.Cube;
                            }
                        }
                    }

                    for (double x = minBound.X; x < maxBound.X; x++)
                    {
                        for (double y = minBound.Y; y < maxBound.Y; y++)
                        {
                            Point3D roundPointA = new(x, y, zMin);
                            Point3D roundPointB = new(x, y, zMax);
                            if (MeshHelper.RayIntersectTriangle(rayPoints, roundPointA, roundPointB, out Point3D intersect, out _)) // Ray
                            {
                                cubic[(int)Math.Floor(intersect.X) - xMin][(int)Math.Floor(intersect.Y) - yMin][(int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                            }
                        }
                    }
                }
            }

            #endregion

            return cubic;
        }

        #endregion
        #region ProccessCubic


        private static CubeType ProcessCubicRange(CubeType[][][] cubic, int xCount, int yCount, int zCount, Action<int, int, int> action = null)
        {
            int x = 0, y = 0, z = 0;
            var cRange = from X in Enumerable.Range(x, xCount)
                         from Y in Enumerable.Range(y, yCount)
                         from Z in Enumerable.Range(z, zCount)
                         select new { X, Y, Z, Value = cubic[X][Y][Z] };
                         
            CubeType cubeType = CubeType.None;
            _ = Parallel.ForEach(cRange, item =>
             {
                 if (item.Value != CubeType.None)
                 {
                     cubeType = item.Value;
                     action?.Invoke(item.X, item.Y, item.Z);
                 }
             });

            return cubeType;
        }


        #region GetNeighbors
        private static IEnumerable<Vector3I> GetNeighbors(Vector3I point, int xCount, int yCount, int zCount)
        {
            Vector3I[] directions =
            [
                new(-1, 0, 0),
                new(1, 0, 0),
                new(0, -1, 0),
                new(0, 1, 0),
                new(0, 0, -1),
                new(0, 0, 1)
            ];

            foreach (Vector3I dir in directions)
            {
                Vector3I neighbor = point + dir;

                if (neighbor.X >= 0 &&
                    neighbor.Y >= 0 &&
                    neighbor.Z >= 0 &&
                    neighbor.X < xCount &&
                    neighbor.Y < yCount &&
                    neighbor.Z < zCount)
                {
                    yield return neighbor;
                }
            }
        }

        #endregion

        #region CrawlExterior

        public static void CrawlExterior(CubeType[][][] cubic)
        {
            int xCount = cubic.Length;
            int yCount = cubic[0].Length;
            int zCount = cubic[0][0].Length;

            ConcurrentQueue<Vector3I> list = [];

            // Add basic check points from the corner blocks.
            Vector3I[] cornerPoints =
            [
                new(0, 0, 0),
                new(xCount - 1, 0, 0),
                new(0, yCount - 1, 0),
                new(0, 0, zCount - 1),
                new(xCount - 1, yCount - 1, 0),
                new(0, yCount - 1, zCount - 1),
                new(xCount - 1, 0, zCount - 1),
                new(xCount - 1, yCount - 1, zCount - 1)
            ];
            foreach (var point in from Vector3I point in cornerPoints
                                  where cubic[point.X][point.Y][point.Z] == CubeType.None
                                  select point)
            {
                list.Enqueue(point);
            }

            foreach (var item in from item in list
                                 where cubic[item.X][item.Y][item.Z] == CubeType.None
                                 select item)
            {
                cubic[item.X][item.Y][item.Z] = CubeType.Exterior;
                var neighbors = GetNeighbors(item, xCount, yCount, zCount).Where(n => cubic[n.X][n.Y][n.Z] == CubeType.None).ToList();
                if (neighbors.Count > 0)
                {
                    foreach (Vector3I neighbor in neighbors)
                    {
                        list.Enqueue(neighbor);
                    }
                }
            }

            _ = ProcessCubicRange(cubic, xCount, yCount, zCount, (x, y, z) =>
            {
                if (cubic[x][y][z] == CubeType.None)
                {
                    cubic[x][y][z] = CubeType.Interior;
                }
                else if (cubic[x][y][z] == CubeType.Exterior)
                {
                    cubic[x][y][z] = CubeType.None;
                }
            });
        }

        #endregion

        #region CountCubic

        public static Dictionary<CubeType, int> CountCubic(CubeType[][][] cubic)
        {
            Dictionary<CubeType, int> assetCount = [];
            int xCount = cubic.Length,
                yCount = cubic[0].Length,
                zCount = cubic[0][0].Length;
            _ = ProcessCubicRange(cubic, xCount, yCount, zCount, (x, y, z) =>
            {
                if (cubic[x][y][z] != CubeType.None)
                {
                    CubeType[] cy = [cubic[x][y][z]];
                    for (z = 0; z < zCount; z++)
                    {
                        if (assetCount.ContainsKey(cy[z]))
                        {
                            assetCount[cy[z]]++;
                        }
                        else
                        {
                            assetCount.Add(cy[z], 1);
                        }
                    }
                }
            });

            return assetCount;
        }

        #endregion

        #region SurfaceCalculated
        // TODO: Implement this method to calculate the surface area of the cubic blocks.
        // This method should iterate through all the cubic blocks and calculate the surface area based on the type of each block.
        #endregion


        #region CalculateAddedSlopes

        public static void CalculateAddedSlopes(CubeType[][][] cubic, Action incrementProgress = null)
        {

            int xCount = cubic.Length,
                yCount = cubic[0].Length,
                zCount = cubic[0][0].Length;

            _ = ProcessCubicRange(cubic, xCount, yCount, zCount, (x, y, z) =>
            {
                incrementProgress?.Invoke();
                if (cubic[x][y][z] == CubeType.Cube)
                {

                    CubeType result = DetermineAddedSlopeType(cubic, x, y, z, xCount, yCount, zCount);
                    if (result != CubeType.None)
                        cubic[x][y][z] = result;
                }
            });

        }

        public static List<CubeType> Slopes =
        [
            CubeType.SlopeCenterFrontTop,
            CubeType.SlopeLeftFrontCenter,
            CubeType.SlopeRightFrontCenter,
            CubeType.SlopeCenterFrontBottom,
            CubeType.SlopeLeftCenterTop,
            CubeType.SlopeRightCenterTop,
            CubeType.SlopeLeftCenterBottom,
            CubeType.SlopeRightCenterBottom,
            CubeType.SlopeCenterBackTop,
            CubeType.SlopeLeftBackCenter,
            CubeType.SlopeRightBackCenter,
            CubeType.SlopeCenterBackBottom,
            CubeType.SlopeLeftBackCenter

        ];

        public static CubeType DetermineAddedSlopeType(CubeType[][][] cubic, int x, int y, int z, int xCount, int yCount, int zCount)
        {
            (CubeType slopeType, int cx, int cy, int cz)[] slopeChecks =
            [
                (CubeType.SlopeCenterFrontTop, 0, 1, 1),
                (CubeType.SlopeLeftFrontCenter, -1, 1, 0),
                (CubeType.SlopeRightFrontCenter, 1, 1, 0),
                (CubeType.SlopeCenterFrontBottom, 0, 1, -1),
                (CubeType.SlopeLeftCenterTop, -1, 0, 1),
                (CubeType.SlopeRightCenterTop, 1, 0, 1),
                (CubeType.SlopeLeftCenterBottom, -1, 0, -1),
                (CubeType.SlopeRightCenterBottom, 1, 0, -1),
                (CubeType.SlopeCenterBackTop, 0, -1, 1),
                (CubeType.SlopeLeftBackCenter, -1, -1, 0),
                (CubeType.SlopeRightBackCenter, 1, -1, 0),
                (CubeType.SlopeCenterBackBottom, 0, -1, -1)
            ];
            foreach ((CubeType slopeType, int cx, int cy, int cz) in slopeChecks)
            {
                if (CheckAdjacentCubic1(cubic, x, y, z, xCount, yCount, zCount, cx, cy, cz, CubeType.Cube))
                    return slopeType;
            }
            return CubeType.None;
        }

        #endregion

        #region CalculateAddedCorners
        
        public static void CalculateAddedCorners(CubeType[][][] cubic, Action incrementProgress = null)
        {
            int xCount = cubic.Length,
                yCount = cubic[0].Length,
                zCount = cubic[0][0].Length;

            _ = ProcessCubicRange(cubic, xCount, yCount, zCount, (x, y, z) =>
           {
               incrementProgress?.Invoke();
               if (cubic[x][y][z] == CubeType.None)
               {
                   CubeType result = DetermineCornerType(cubic, x, y, z, xCount, yCount, zCount);
                   if (result != CubeType.None)
                       cubic[x][y][z] = result;
               }
           });
        }

        public static List<CubeType> Corners =
        [
            CubeType.NormalCornerLeftFrontTop,
            CubeType.NormalCornerRightFrontTop,
            CubeType.NormalCornerLeftFrontBottom,
            CubeType.NormalCornerRightFrontBottom,
            CubeType.NormalCornerLeftBackTop,
            CubeType.NormalCornerRightBackTop,
            CubeType.NormalCornerLeftBackBottom,
            CubeType.NormalCornerRightBackBottom
        ];

        public static CubeType DetermineCornerType(CubeType[][][] cubic, int x, int y, int z, int xCount, int yCount, int zCount)
        {
            (CubeType result, (int cx1, int cy1, int cz, CubeType type1), (int cx2, int cy2, int cz2, CubeType type2))[] cornerChecks =
            [
                (CubeType.NormalCornerLeftFrontTop, (0, 0, +1, CubeType.SlopeLeftFrontCenter), (-1, 0, 0, CubeType.SlopeCenterFrontTop)),
                (CubeType.NormalCornerRightFrontTop, (0, 0, +1, CubeType.SlopeRightFrontCenter), (+1, 0, 0, CubeType.SlopeCenterFrontTop)),
                (CubeType.NormalCornerLeftFrontBottom, (0, 0, -1, CubeType.SlopeLeftFrontCenter), (-1, 0, 0, CubeType.SlopeCenterFrontBottom)),
                (CubeType.NormalCornerRightFrontBottom, (0, 0, -1, CubeType.SlopeRightFrontCenter), (+1, 0, 0, CubeType.SlopeCenterFrontBottom)),
                (CubeType.NormalCornerLeftBackTop, (0, 0, +1, CubeType.SlopeLeftBackCenter), (-1, 0, 0, CubeType.SlopeCenterBackTop)),
                (CubeType.NormalCornerRightBackTop, (0, 0, +1, CubeType.SlopeRightBackCenter), (+1, 0, 0, CubeType.SlopeCenterBackTop)),
                (CubeType.NormalCornerLeftBackBottom, (0, 0, -1, CubeType.SlopeLeftBackCenter), (-1, 0, 0, CubeType.SlopeCenterBackBottom)),
                (CubeType.NormalCornerRightBackBottom, (0, 0, -1, CubeType.SlopeRightBackCenter), (+1, 0, 0, CubeType.SlopeCenterBackBottom))
            ];

            foreach ((CubeType result, (int cx1, int cy1, int cz1, CubeType type1) check1, (int cx2, int cy2, int cz2, CubeType type2) check2) in cornerChecks)
            {
                if (CheckAdjacentCubic2(cubic, x, y, z, xCount, yCount, zCount,
                    check1.cx1, check1.cy1, check1.cz1, check1.type1,
                    check2.cx2, check2.cy2, check2.cz2, check2.type2))
                    return result;
            }

            (CubeType result, (int cx1, int cy1, int cz1, CubeType type1), (int cx2, int cy2, int cz2, CubeType type2), (int cx3, int cy3, int cz3, CubeType type3))[] tripleChecks =
            [
                (CubeType.NormalCornerRightBackBottom, (+1, 0, 0, CubeType.InverseCornerLeftFrontTop), 
                                                       (0, -1, 0, CubeType.InverseCornerLeftFrontTop), 
                                                       (0, 0, -1, CubeType.InverseCornerLeftFrontTop)),
                (CubeType.NormalCornerLeftFrontTop, (-1, 0, 0, CubeType.InverseCornerRightBackBottom), 
                                                    (0, +1, 0, CubeType.InverseCornerRightBackBottom), 
                                                    (0, 0, +1, CubeType.InverseCornerRightBackBottom))
            ];

            foreach ((CubeType result, (int cx1, int cy1, int cz1, CubeType type1) check1, (int cx2, int cy2, int cz2, CubeType type2) check2, (int cx3, int cy3, int cz3, CubeType type3) check3) in tripleChecks)
            {
                if (CheckAdjacentCubic3(cubic, x, y, z, xCount, yCount, zCount,
                    check1.cx1, check1.cy1, check1.cz1, check1.type1,
                    check2.cx2, check2.cy2, check2.cz2, check2.type2,
                    check3.cx3, check3.cy3, check3.cz3, check3.type3))
                    return result;
            }
            return CubeType.None;
        }

        #endregion

        #region CalculateAddedInverseCorners

        public static void CalculateAddedInverseCorners(CubeType[][][] cubic, Action incrementProgress = null)
        {

            int xCount = cubic.Length,
                yCount = cubic[0].Length,
                zCount = cubic[0][0].Length;


            _ = ProcessCubicRange(cubic, xCount, yCount, zCount, (x, y, z) =>
            {
                incrementProgress?.Invoke();
                if (cubic[x][y][z] == CubeType.None)
                {
                    CubeType result = DetermineInverseCornerType(cubic, x, y, z, xCount, yCount, zCount);
                    if (result != CubeType.None)
                        cubic[x][y][z] = result;
                }
            });
        }

        public static List<CubeType> InverseCorners =
        [
                CubeType.InverseCornerLeftFrontTop,
                CubeType.InverseCornerRightBackTop,
                CubeType.InverseCornerLeftBackTop,
                CubeType.InverseCornerRightFrontTop,
                CubeType.InverseCornerLeftBackBottom,
                CubeType.InverseCornerRightBackBottom,
                CubeType.InverseCornerLeftFrontBottom,
                CubeType.InverseCornerRightFrontBottom,
        ];

        public static CubeType DetermineInverseCornerType(CubeType[][][] cubic, int x, int y, int z, int xCount, int yCount, int zCount)
        {
            (CubeType result, (int dx1, int dy1, int dz1), (int dx2, int dy2, int dz2), (int dx3, int dy3, int dz3))[] inverseCornerChecks =
            [
                (CubeType.InverseCornerLeftFrontTop,     (+1, 0, 0), (0, -1, 0), (0, 0, -1)),
                (CubeType.InverseCornerRightBackTop,     (-1, 0, 0), (0, +1, 0), (0, 0, -1)),
                (CubeType.InverseCornerLeftBackTop,      (+1, 0, 0), (0, +1, 0), (0, 0, -1)),
                (CubeType.InverseCornerRightFrontTop,    (-1, 0, 0), (0, -1, 0), (0, 0, -1)),
                (CubeType.InverseCornerLeftBackBottom,   (+1, 0, 0), (0, +1, 0), (0, 0, +1)),
                (CubeType.InverseCornerRightBackBottom,  (-1, 0, 0), (0, +1, 0), (0, 0, +1)),
                (CubeType.InverseCornerLeftFrontBottom,  (+1, 0, 0), (0, -1, 0), (0, 0, +1)),
                (CubeType.InverseCornerRightFrontBottom, (-1, 0, 0), (0, -1, 0), (0, 0, +1))
            ];
            foreach ((CubeType result, (int dx1, int dy1, int dz1) check1, (int dx2, int dy2, int dz2) check2, (int dx3, int dy3, int dz3) check3) in inverseCornerChecks)
            {
                if (CheckAdjacentCubic3(cubic, x, y, z, xCount, yCount, zCount,
                    check1.dx1, check1.dy1, check1.dz1, CubeType.Cube,
                    check2.dx2, check2.dy2, check2.dz2, CubeType.Cube,
                    check3.dx3, check3.dy3, check3.dz3, CubeType.Cube))
                    return result;
            }
            return CubeType.None;
        }

        #endregion

        #region CheckAdjacentCubic

        private static bool IsValidRange(int x, int y, int z, int xCount, int yCount, int zCount, int xDelta, int yDelta, int zDelta)
        {
            return x + xDelta >= 0 && x + xDelta < xCount &&
                   y + yDelta >= 0 && y + yDelta < yCount &&
                   z + zDelta >= 0 && z + zDelta < zCount;
        }

        private static bool CheckAdjacentCubic(CubeType[][][] cubic, int x, int y, int z, int xCount, int yCount, int zCount, int xDelta, int yDelta, int zDelta, CubeType cubeType)
        {
            if (!IsValidRange(x, y, z, xCount, yCount, zCount, xDelta, yDelta, zDelta))
                return false;

            bool xMatch = xDelta != 0 && cubic[x + xDelta][y][z] == cubeType;
            bool yMatch = yDelta != 0 && cubic[x][y + yDelta][z] == cubeType;
            bool zMatch = zDelta != 0 && cubic[x][y][z + zDelta] == cubeType;

            return (xMatch && yMatch && zDelta == 0) ||
                   (xMatch && zMatch && yDelta == 0) ||
                   (yMatch && zMatch && xDelta == 0) ||
                   (xMatch && yMatch && zMatch);
        }

        private static bool CheckAdjacentCubic1(CubeType[][][] cubic, int x, int y, int z, int xCount, int yCount, int zCount,
           int xDelta, int yDelta, int zDelta, CubeType cubeType)
        {
            if (IsValidRange(x, y, z, xCount, yCount, zCount, xDelta, yDelta, zDelta))
            {
                return cubic[x + xDelta][y + yDelta][z + zDelta] == cubeType;
            }

            return false;
        }

        private static bool CheckAdjacentCubic2(CubeType[][][] cubic, int x, int y, int z, int xCount, int yCount, int zCount,
            int xDelta1, int yDelta1, int zDelta1, CubeType cubeType1,
            int xDelta2, int yDelta2, int zDelta2, CubeType cubeType2)
        {
            if (IsValidRange(x, y, z, xCount, yCount, zCount, xDelta1, yDelta1, zDelta1) && IsValidRange(x, y, z, xCount, yCount, zCount, xDelta2, yDelta2, zDelta2))
            {
                return cubic[x + xDelta1][y + yDelta1][z + zDelta1] == cubeType1 && cubic[x + xDelta2][y + yDelta2][z + zDelta2] == cubeType2;
            }

            return false;
        }

        private static bool CheckAdjacentCubic3(CubeType[][][] cubic, int x, int y, int z, int xCount, int yCount, int zCount,
            int xDelta1, int yDelta1, int zDelta1, CubeType cubeType1,
            int xDelta2, int yDelta2, int zDelta2, CubeType cubeType2,
            int xDelta3, int yDelta3, int zDelta3, CubeType cubeType3)
        {
            if (IsValidRange(x, y, z, xCount, yCount, zCount, xDelta1, yDelta1, zDelta1)
                && IsValidRange(x, y, z, xCount, yCount, zCount, xDelta2, yDelta2, zDelta2)
                && IsValidRange(x, y, z, xCount, yCount, zCount, xDelta3, yDelta3, zDelta3))
            {
                return cubic[x + xDelta1][y + yDelta1][z + zDelta1] == cubeType1
                    && cubic[x + xDelta2][y + yDelta2][z + zDelta2] == cubeType2
                    && cubic[x + xDelta3][y + yDelta3][z + zDelta3] == cubeType3;
            }

            return false;
        }

        #endregion

        #region BuildStructureFromCubic

        internal static void BuildStructureFromCubic(MyObjectBuilder_CubeGrid entity, CubeType[][][] cubic, bool fillObject, SubtypeId blockType, SubtypeId slopeBlockType, SubtypeId cornerBlockType, SubtypeId inverseCornerBlockType, Action incrementProgress = null)
        {
            int xCount = cubic.Length,
                yCount = cubic[0].Length,
                zCount = cubic[0][0].Length;

            _ = ProcessCubicRange(cubic, xCount, yCount, zCount, (x, y, z) =>
            {
                incrementProgress?.Invoke();
                CubeType cubeType = cubic[x][y][z];

                if (cubeType == CubeType.None || (cubeType == CubeType.Interior && !fillObject))
                {
                    return;
                }

                MyObjectBuilder_CubeBlock newCube = CreateCubeBlock(cubeType, blockType, slopeBlockType, cornerBlockType, inverseCornerBlockType, fillObject, x, y, z);
                entity.CubeBlocks.Add(newCube);

                if (cubeType == CubeType.Interior && fillObject)
                {
                    cubic[x][y][z] = CubeType.Cube;
                }
            });
        }

        #endregion

        #region CalculateSubtractedCorners

        // Experimental code.
        public static void CalculateSubtractedCorners(CubeType[][][] cubic, Action incrementProgress = null)
        {
            int xCount = cubic.Length,
                yCount = cubic[0].Length,
                zCount = cubic[0][0].Length;

            _ = ProcessCubicRange(cubic, xCount, yCount, zCount, (x, y, z) =>
            {
                incrementProgress?.Invoke();
                if (cubic[x][y][z] == CubeType.Cube)
                {
                    CubeType result = DetermineSubtractedCornerType(cubic, x, y, z, xCount, yCount, zCount);
                    if (result != CubeType.Cube)
                        cubic[x][y][z] = result;
                }
            });
        }

        private static CubeType DetermineSubtractedCornerType(CubeType[][][] cubic, int x, int y, int z, int xCount, int yCount, int zCount)
        {
            (CubeType cornerType, (int dx1, int dy1, int dz1), (int dx2, int dy2, int dz2), (int dx3, int dy3, int dz3))[] subtractedCornerChecks =
            [
                (CubeType.NormalCornerLeftFrontTop,     (-1, 0, 0), (0, +1, 0), (0, 0, +1)),
                (CubeType.NormalCornerRightFrontTop,    (+1, 0, 0), (0, +1, 0), (0, 0, +1)),
                (CubeType.NormalCornerLeftBackTop,      (-1, 0, 0), (0, -1, 0), (0, 0, +1)),
                (CubeType.NormalCornerRightBackTop,     (+1, 0, 0), (0, -1, 0), (0, 0, +1)),
                (CubeType.NormalCornerLeftFrontBottom,  (-1, 0, 0), (0, +1, 0), (0, 0, -1)),
                (CubeType.NormalCornerRightFrontBottom, (+1, 0, 0), (0, +1, 0), (0, 0, -1)),
                (CubeType.NormalCornerLeftBackBottom,   (-1, 0, 0), (0, -1, 0), (0, 0, -1)),
                (CubeType.NormalCornerRightBackBottom,  (+1, 0, 0), (0, -1, 0), (0, 0, -1))
            ];

            foreach ((CubeType cornerType, (int dx1, int dy1, int dz1) check1, (int dx2, int dy2, int dz2) check2, (int dx3, int dy3, int dz3) check3) in subtractedCornerChecks)
            {
                if (CheckAdjacentCubic3(cubic, x, y, z, xCount, yCount, zCount,
                    check1.dx1, check1.dy1, check1.dz1, CubeType.None,
                    check2.dx2, check2.dy2, check2.dz2, CubeType.None,
                    check3.dx3, check3.dy3, check3.dz3, CubeType.None))
                {
                    return cornerType;
                }
            }
            return CubeType.Cube;
        }

        #endregion

        #region CalculateSubtractedSlopes

        public static void CalculateSubtractedSlopes(CubeType[][][] cubic, Action incrementProgress = null)
        {
            int xCount = cubic.Length, yCount = cubic[0].Length, zCount = cubic[0][0].Length;
            _ = ProcessCubicRange(cubic, xCount, yCount, zCount, (x, y, z) =>
            {
                incrementProgress?.Invoke();
                if (cubic[x][y][z] == CubeType.Cube)
                {
                    CubeType result = DetermineSubtractedSlopeType(cubic, x, y, z, xCount, yCount, zCount);
                    if (result != CubeType.Cube)
                        cubic[x][y][z] = result;
                }
            });
        }

        public static CubeType DetermineSubtractedSlopeType(CubeType[][][] cubic, int x, int y, int z, int xCount, int yCount, int zCount)
        {
            (CubeType slopeType, (int dx, int dy, int dz) cubeCheck, (int dx, int dy, int dz) noneCheck)[] slopeChecks =
            [
                    (CubeType.SlopeCenterFrontTop,      (0, +1, +1), (0, -1, -1)),
                    (CubeType.SlopeLeftFrontCenter,     (-1, +1, 0), (+1, -1, 0)),
                    (CubeType.SlopeRightFrontCenter,    (+1, +1, 0), (-1, -1, 0)),
                    (CubeType.SlopeCenterFrontBottom,   (0, +1, -1), (0, -1, +1)),
                    (CubeType.SlopeLeftCenterTop,       (-1, 0, +1), (+1, 0, -1)),
                    (CubeType.SlopeRightCenterTop,      (+1, 0, +1), (-1, 0, -1)),
                    (CubeType.SlopeLeftCenterBottom,    (-1, 0, -1), (+1, 0, +1)),
                    (CubeType.SlopeRightCenterBottom,   (+1, 0, -1), (-1, 0, +1)),
                    (CubeType.SlopeCenterBackTop,       (0, -1, +1), (0, +1, -1)),
                    (CubeType.SlopeLeftBackCenter,      (-1, -1, 0), (+1, +1, 0)),
                    (CubeType.SlopeRightBackCenter,     (+1, -1, 0), (-1, +1, 0)),
                    (CubeType.SlopeCenterBackBottom,    (0, -1, -1), (0, +1, +1))
            ];

            foreach ((CubeType slopeType, (int dx, int dy, int dz) cubeCheck, (int dx, int dy, int dz) noneCheck) in slopeChecks)
            {
                if (CheckAdjacentCubic(cubic, x, y, z, xCount, yCount, zCount,
                    cubeCheck.dx, cubeCheck.dy, cubeCheck.dz, CubeType.Cube) &&
                    CheckAdjacentCubic(cubic, x, y, z, xCount, yCount, zCount,
                    noneCheck.dx, noneCheck.dy, noneCheck.dz, CubeType.None))
                {
                    return slopeType;
                }
            }

            return CubeType.Cube;
        }

        #endregion

        #region CalculateSubtractedInverseCorners

        public static void CalculateSubtractedInverseCorners(CubeType[][][] cubic, Action incrementProgress = null)
        {

            int xCount = cubic.Length,
                yCount = cubic[0].Length,
                zCount = cubic[0][0].Length;

            _ = ProcessCubicRange(cubic, xCount, yCount, zCount, (x, y, z) =>
            {
                incrementProgress?.Invoke();
                if (cubic[x][y][z] == CubeType.Cube)
                {
                    CubeType result = DetermineSubtractedInverseCornerType(cubic, x, y, z, xCount, yCount, zCount);
                    if (result != CubeType.Cube)
                        cubic[x][y][z] = result;
                }
            });
        }

        private static CubeType DetermineSubtractedInverseCornerType(CubeType[][][] cubic, int x, int y, int z, int xCount, int yCount, int zCount)
        {
            (CubeType cornerType, (int dx1, int dy1, int dz1), (int dx2, int dy2, int dz2), (int dx3, int dy3, int dz3))[] inverseCornerChecks =
            [
                (CubeType.InverseCornerLeftFrontTop,     (+1, 0, 0), (0, -1, 0), (0, 0, -1)),
                (CubeType.InverseCornerRightBackTop,     (-1, 0, 0), (0, +1, 0), (0, 0, -1)),
                (CubeType.InverseCornerLeftBackTop,      (+1, 0, 0), (0, +1, 0), (0, 0, -1)),
                (CubeType.InverseCornerRightFrontTop,    (-1, 0, 0), (0, -1, 0), (0, 0, -1)),
                (CubeType.InverseCornerLeftBackBottom,   (+1, 0, 0), (0, +1, 0), (0, 0, +1)),
                (CubeType.InverseCornerRightBackBottom,  (-1, 0, 0), (0, +1, 0), (0, 0, +1)),
                (CubeType.InverseCornerLeftFrontBottom,  (+1, 0, 0), (0, -1, 0), (0, 0, +1)),
                (CubeType.InverseCornerRightFrontBottom, (-1, 0, 0), (0, -1, 0), (0, 0, +1))
            ];
            foreach ((CubeType cornerType, (int dx1, int dy1, int dz1) check1, (int dx2, int dy2, int dz2) check2, (int dx3, int dy3, int dz3) check3) in inverseCornerChecks)
            {
                if (CheckAdjacentCubic3(cubic, x, y, z, xCount, yCount, zCount,
                    check1.dx1, check1.dy1, check1.dz1, CubeType.None,
                    check2.dx2, check2.dy2, check2.dz2, CubeType.None,
                    check3.dx3, check3.dy3, check3.dz3, CubeType.None))
                {
                    return cornerType;
                }
            }
            return CubeType.Cube;
        }

        #endregion

        private static MyObjectBuilder_CubeBlock CreateCubeBlock(CubeType cubeType, SubtypeId blockType, SubtypeId slopeBlockType, SubtypeId cornerBlockType, SubtypeId inverseCornerBlockType, bool fillObject, int x, int y, int z)
        {
            return new MyObjectBuilder_CubeBlock
            {
                SubtypeName = GetSubtypeName(cubeType, blockType, slopeBlockType, cornerBlockType, inverseCornerBlockType, fillObject),
                EntityId = 0,
                BlockOrientation = GetCubeOrientation(cubeType),
                Min = new Vector3I(x, y, z)
            };
        }

        private static string GetSubtypeName(CubeType cubeType, SubtypeId blockType, SubtypeId slopeBlockType, SubtypeId cornerBlockType, SubtypeId inverseCornerBlockType, bool fillObject)
        {
            return cubeType switch
            {
                _ when cubeType.ToString().StartsWith("Cube") => blockType.ToString(),
                _ when cubeType.ToString().StartsWith("Slope") => slopeBlockType.ToString(),
                _ when cubeType.ToString().StartsWith("NormalCorner") => cornerBlockType.ToString(),
                _ when cubeType.ToString().StartsWith("InverseCorner") => inverseCornerBlockType.ToString(),
                CubeType.Interior when fillObject => blockType.ToString(),
                _ => throw new InvalidOperationException($"Unsupported CubeType: {cubeType}")
            };
        }

        #endregion
        #region SetCubeOrientation

        internal static readonly Dictionary<CubeType, SerializableBlockOrientation> CubeOrientations = new()
        {
            // Cube Armor orientation (can be removed if generic works for all)
            {CubeType.Cube, new(Direction.Forward, Direction.Up)},

            // Slope Armor orientations (can be removed if generic works for all)
            {CubeType.SlopeCenterBackTop, new SerializableBlockOrientation(Direction.Down, Direction.Forward)}, // -90 around X
            {CubeType.SlopeRightBackCenter, new SerializableBlockOrientation(Direction.Down, Direction.Left)},
            {CubeType.SlopeLeftBackCenter, new SerializableBlockOrientation(Direction.Down, Direction.Right)},
            {CubeType.SlopeCenterBackBottom, new SerializableBlockOrientation(Direction.Forward, Direction.Up)}, // no rotation
            {CubeType.SlopeRightCenterTop, new SerializableBlockOrientation(Direction.Backward, Direction.Left)},
            {CubeType.SlopeLeftCenterTop, new SerializableBlockOrientation(Direction.Backward, Direction.Right)},
            {CubeType.SlopeRightCenterBottom, new SerializableBlockOrientation(Direction.Forward, Direction.Left)}, // +90 around Z
            {CubeType.SlopeLeftCenterBottom, new SerializableBlockOrientation(Direction.Forward, Direction.Right)}, // -90 around Z
            {CubeType.SlopeCenterFrontTop, new SerializableBlockOrientation(Direction.Backward, Direction.Down)}, // 180 around X
            {CubeType.SlopeRightFrontCenter, new SerializableBlockOrientation(Direction.Up, Direction.Left)},
            {CubeType.SlopeLeftFrontCenter, new SerializableBlockOrientation(Direction.Up, Direction.Right)},
            {CubeType.SlopeCenterFrontBottom, new SerializableBlockOrientation(Direction.Up, Direction.Backward)},// +90 around X

             // Probably got the names of these all messed up in relation to their actual orientation.
            {CubeType.NormalCornerLeftFrontTop, new SerializableBlockOrientation(Direction.Backward, Direction.Right)},
            {CubeType.NormalCornerRightFrontTop, new SerializableBlockOrientation(Direction.Backward, Direction.Down)},	// 180 around X
            {CubeType.NormalCornerLeftBackTop, new SerializableBlockOrientation(Direction.Down, Direction.Right)},
            {CubeType.NormalCornerRightBackTop, new SerializableBlockOrientation(Direction.Down, Direction.Forward)},	// -90 around X
            {CubeType.NormalCornerLeftFrontBottom, new SerializableBlockOrientation(Direction.Up, Direction.Right)},
            {CubeType.NormalCornerRightFrontBottom, new SerializableBlockOrientation(Direction.Up, Direction.Backward)},// +90 around X
            {CubeType.NormalCornerLeftBackBottom, new SerializableBlockOrientation(Direction.Forward, Direction.Right)},// -90 around Z
            {CubeType.NormalCornerRightBackBottom, new SerializableBlockOrientation(Direction.Forward, Direction.Up)}, // no rotation

            {CubeType.InverseCornerLeftFrontTop, new SerializableBlockOrientation(Direction.Backward, Direction.Right)},
            {CubeType.InverseCornerRightFrontTop, new SerializableBlockOrientation(Direction.Backward, Direction.Down)},// 180 around X
            {CubeType.InverseCornerLeftBackTop, new SerializableBlockOrientation(Direction.Down, Direction.Right)},
            {CubeType.InverseCornerRightBackTop, new SerializableBlockOrientation(Direction.Down, Direction.Forward)},// -90 around X
            {CubeType.InverseCornerLeftFrontBottom, new SerializableBlockOrientation(Direction.Up, Direction.Right)},
            {CubeType.InverseCornerRightFrontBottom, new SerializableBlockOrientation(Direction.Up, Direction.Backward)}, // +90 around X
            {CubeType.InverseCornerLeftBackBottom, new SerializableBlockOrientation(Direction.Forward, Direction.Right)},// -90 around Z
            {CubeType.InverseCornerRightBackBottom, new SerializableBlockOrientation(Direction.Forward, Direction.Up)},// no rotation
        };

        public static SerializableBlockOrientation GetCubeOrientation(CubeType type)
        {
            if (CubeOrientations.TryGetValue(type, out SerializableBlockOrientation orientation))
                return orientation;

            // Fallback: Try to infer orientation for unknown types
            return new SerializableBlockOrientation(Direction.Forward, Direction.Up);//    throw new NotImplementedException(string.Format($"SetCubeOrientation of type [{type}] not yet implemented."));
        }

        #endregion

        #region TestObjects

        internal static CubeType[][][] TestCreateSplayedDiagonalPlane()
        {
            // Splayed diagonal plane.
            int max = 40;
            CubeType[][][] cubic = ArrayHelper.Create<CubeType>(max, max, max);

            for (int z = 0; z < max; z++)
            {
                for (int j = 0; j < max; j++)
                {
                    AddCubeIfValid(cubic, j + z, j, z, max);          // Plane 1
                    AddCubeIfValid(cubic, j, j + z, z, max);          // Plane 2
                    AddCubeIfValid(cubic, j + z, max - j, z, max);    // Plane 3
                    AddCubeIfValid(cubic, j, max - (j + z), z, max);  // Plane 4
                }
            }

            return cubic;
        }

        private static void AddCubeIfValid(CubeType[][][] cubic, int x, int y, int z, int max)
        {
            if (x >= 0 && y >= 0 && z >= 0 && x < max && y < max && z < max)
            {
                cubic[x][y][z] = CubeType.Cube;
            }
        }

        internal static CubeType[][][] TestCreateSlopedDiagonalPlane()
        {
            // Sloped diagonal plane.
            int max = 20;
            CubeType[][][] cubic = ArrayHelper.Create<CubeType>(max, max, max);
            int dx = 1;
            int dy = 6;
            int dz = 0;

            for (int i = 0; i < max; i++)
            {
                for (int j = 0; j < max; j++)
                {
                    if (dx + j >= 0 && dy + j - i >= 0 && dz + i >= 0 &&
                        dx + j < max && dy + j - i < max && dz + i < max)
                    {
                        cubic[dx + j][dy + j - i][dz + i] = CubeType.Cube;
                    }
                }
            }
            return cubic;
        }

        internal static CubeType[][][] TestCreateStaggeredStar()
        {
            // Staggered star.

            CubeType[][][] cubic = ArrayHelper.Create<CubeType>(9, 9, 9);

            for (int i = 2; i < 7; i++)
            {
                for (int j = 2; j < 7; j++)
                {
                    cubic[i][j][4] = CubeType.Cube;
                    cubic[i][4][j] = CubeType.Cube;
                    cubic[4][i][j] = CubeType.Cube;
                }
            }

            for (int i = 0; i < 9; i++)
            {
                cubic[i][4][4] = CubeType.Cube;
                cubic[4][i][4] = CubeType.Cube;
                cubic[4][4][i] = CubeType.Cube;
            }

            return cubic;
        }

        internal static CubeType[][][] TestCreateTrayShape()
        {
            // Tray shape

            int max = 20;
            int offset = 5;

            CubeType[][][] cubic = ArrayHelper.Create<CubeType>(max, max, max);

            for (int x = 0; x < max; x++)
            {
                for (int y = 0; y < max; y++)
                {
                    cubic[2][x][y] = CubeType.Cube;
                }
            }

            for (int z = 1; z < 4; z += 2)
            {
                for (int i = 0; i < max; i++)
                {
                    cubic[z][i][0] = CubeType.Cube;
                    cubic[z][i][max - 1] = CubeType.Cube;
                    cubic[z][0][i] = CubeType.Cube;
                    cubic[z][max - 1][i] = CubeType.Cube;
                }

                for (int i = 0 + offset; i < max - offset; i++)
                {
                    cubic[z][i][i] = CubeType.Cube;
                    cubic[z][max - i - 1][i] = CubeType.Cube;
                }
            }

            return cubic;
        }

        #endregion
    }
}
