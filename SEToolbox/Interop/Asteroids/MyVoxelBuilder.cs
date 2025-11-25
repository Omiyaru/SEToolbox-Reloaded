using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using SEToolbox.Support;
using VRage.Voxels;
using VRageMath;

namespace SEToolbox.Interop.Asteroids
{
    public static class MyVoxelBuilder
    {
        private static readonly object Locker = new();

        public static void ConvertAsteroid(string loadFile, string saveFile, string defaultMaterial, string material)
        {
            MyVoxelMapBase voxelMap = new();
            voxelMap.Load(loadFile);
            voxelMap.ForceBaseMaterial(defaultMaterial, material);
            voxelMap.Save(saveFile);
            voxelMap.Dispose();
        }


        public static void StripMaterial(string loadFile, string saveFile, string stripMaterial, string replaceFillMaterial)
        {
            MyVoxelMapBase voxelMap = new();
            voxelMap.Load(loadFile);
            voxelMap.RemoveContent(stripMaterial, replaceFillMaterial);
            voxelMap.Save(saveFile);
            voxelMap.Dispose();
        }

        #region BuildAsteroid Standard Tools

        public static MyVoxelMapBase BuildAsteroidCube(bool multiThread, int width, int height, int depth,
            byte materialIndex, byte faceMaterialIndex, bool hollow = false, int shellWidth = 0, float safeSize = 0f)
        {
            // offset by 1, to allow for the 3 faces on the origin side.
            Vector3I size = new Vector3I(width, height, depth) + 1;

            // offset by 1, to allow for the 3 faces on opposite side.
            Vector3I buildSize = size + 1;

            void CellAction(ref MyVoxelBuilderArgs e)
            {
                if (e.CoordinatePoint.X <= safeSize ||
                    e.CoordinatePoint.Y <= safeSize ||
                    e.CoordinatePoint.Z <= safeSize ||
                    e.CoordinatePoint.X >= size.X - safeSize ||
                    e.CoordinatePoint.Y >= size.Y - safeSize ||
                    e.CoordinatePoint.Z >= size.Z - safeSize)
                {
                    e.Volume = 0x00;
                }
                else if (hollow &&
                    (e.CoordinatePoint.X <= safeSize + shellWidth ||
                     e.CoordinatePoint.Y <= safeSize + shellWidth ||
                     e.CoordinatePoint.Z <= safeSize + shellWidth ||
                     e.CoordinatePoint.X >= size.X - (safeSize + shellWidth) ||
                     e.CoordinatePoint.Y >= size.Y - (safeSize + shellWidth) ||
                     e.CoordinatePoint.Z >= size.Z - (safeSize + shellWidth)))

                {
                    e.Volume = 0xFF;
                }
                else if (hollow)
                {
                    e.Volume = 0x00;
                }
                else// if (!hollow)
                {
                    e.Volume = 0xFF;
                }
            }

            return BuildAsteroid(multiThread, buildSize, materialIndex, faceMaterialIndex, CellAction);
        }

        public static MyVoxelMapBase BuildAsteroidCube(bool multiThread, Vector3I min, Vector3I max, byte materialIndex, byte faceMaterialIndex)
        {
            // correct for allowing sizing.
            Vector3I buildSize = CalcRequiredSize(max);

            void CellAction(ref MyVoxelBuilderArgs e)
            {
                if (e.CoordinatePoint.X < min.X || e.CoordinatePoint.Y < min.Y || e.CoordinatePoint.Z < min.Z
                    || e.CoordinatePoint.X > max.X || e.CoordinatePoint.Y > max.Y || e.CoordinatePoint.Z > max.Z)
                {
                    e.Volume = 0x00;
                }
                else //if (!hollow)
                {
                    e.Volume = 0xFF;
                }
            }

            return BuildAsteroid(multiThread, buildSize, materialIndex, faceMaterialIndex, CellAction);
        }

        public static MyVoxelMapBase BuildAsteroidSphere(bool multiThread, double radius, byte materialIndex, byte faceMaterialIndex,
            bool hollow = false, int shellWidth = 0)
        {
            int length = (int)((radius * 2) + 2);
            Vector3I buildSize = CalcRequiredSize(length);
            Vector3I origin = new(buildSize.X / 2, buildSize.Y / 2, buildSize.Z / 2);

            void CellAction(ref MyVoxelBuilderArgs e)
            {
                VRageMath.Vector3D voxelPosition = e.CoordinatePoint;

                int v = GetSphereVolume(ref voxelPosition, radius, origin);

                if (hollow)
                {
                    int h = GetSphereVolume(ref voxelPosition, radius - shellWidth, origin);
                    e.Volume = (byte)(v - h);
                }
                else
                {
                    e.Volume = (byte)v;
                }
            }

            return BuildAsteroid(multiThread, buildSize, materialIndex, faceMaterialIndex, CellAction);
        }

        public static byte GetSphereVolume(ref VRageMath.Vector3D voxelPosition, double radius, VRageMath.Vector3D center)
        {
            double num = (voxelPosition - center).Length();
            double signedDistance = num - radius;

            signedDistance = MathHelper.Clamp(-signedDistance, -1, 1) * 0.5 + 0.5;

            return (byte)(signedDistance * 255);
        }

        public static MyVoxelMapBase BuildAsteroidFromModel(bool multiThread, string sourceVolumetricFile, byte materialIndex, byte faceMaterialIndex,
            bool fillObject, byte? interiorMaterialIndex, ModelTraceVoxel traceType, double scale, Transform3D transform)
        {
            return BuildAsteroidFromModel(multiThread, sourceVolumetricFile, materialIndex, faceMaterialIndex,
                fillObject, interiorMaterialIndex, traceType, scale, transform, null, null);
        }

        public static MyVoxelMapBase BuildAsteroidFromModel(bool multiThread, string sourceVolumetricFile, byte materialIndex, byte faceMaterialIndex,
            bool fillObject, byte? interiorMaterialIndex, ModelTraceVoxel traceType, double scale, Transform3D transform,
            Action<double, double> resetProgress, Action incrementProgress)
        {
            var volumetricMap = Modelling.ReadVolumetricModel(sourceVolumetricFile, scale, transform, traceType, resetProgress, incrementProgress);
            // these large values were to fix issue with large square gaps in voxlized asteroid model.
            var size = new Vector3I(volumetricMap.Length + 12, volumetricMap[0].Length + 12, volumetricMap[0][0].Length + 12);

            void CellAction(ref MyVoxelBuilderArgs e)
            {
                if (e.CoordinatePoint.X > 5 && e.CoordinatePoint.Y > 5 && e.CoordinatePoint.Z > 5 &&
                    (e.CoordinatePoint.X <= volumetricMap.Length + 5) &&
                    (e.CoordinatePoint.Y <= volumetricMap[0].Length + 5) &&
                    (e.CoordinatePoint.Z <= volumetricMap[0][0].Length + 5))

                {
                    CubeType cube = volumetricMap[e.CoordinatePoint.X - 6][e.CoordinatePoint.Y - 6][e.CoordinatePoint.Z - 6];

                    e.Volume = cube switch
                    {
                        CubeType.Interior when fillObject => 0xff,// 100%
                        CubeType.Cube => 0xff,// 100% "11111111"
                        CubeType c when c.ToString().StartsWith("InverseCorner") => 0xD4,// 83% "11010100"
                        CubeType c when c.ToString().StartsWith("Slope") => 0x7F,// 50% "01111111"
                        CubeType c when c.ToString().StartsWith("NormalCorner") => 0x2B,// 16% "00101011"
                        _ => 0x00,// 0% "00000000"
                    };

                }
                else
                {
                    e.Volume = 0x00;
                }
            }

            return BuildAsteroid(multiThread, size, materialIndex, faceMaterialIndex, CellAction);
        }

        #endregion

        #region BuildAsteroid

        /// <summary>
        /// Builds an asteroid Voxel. Voxel detail will be completed by function callbacks.
        /// This allows for muti-threading, and generating content via algorithims.
        /// </summary>
        public static MyVoxelMapBase BuildAsteroid(bool multiThread, Vector3I size, byte materialIndex, byte? faceMaterialIndex, VoxelBuilderAction func)
        {
            var voxelMap = new MyVoxelMapBase();

            Vector3I buildSize = CalcRequiredSize(size);
            voxelMap.Create(buildSize, materialIndex);
            ProcessAsteroid(voxelMap, multiThread, materialIndex, func, true);

            // This should no longer be required.
            //if (faceMaterialIndex != null)
            //{
            //    voxelMap.ForceVoxelFaceMaterial(faceMaterialIndex.Value);
            //}

            return voxelMap;
        }

        #endregion

        #region ProcessAsteroid

        /// <summary>
        /// Processes an asteroid Voxel using function callbacks.
        /// This allows for muti-threading, and generating content via algorithims.
        /// </summary>

        public static void ProcessAsteroid(MyVoxelMapBase voxelMap, bool multiThread, byte materialIndex, VoxelBuilderAction func, bool readWrite = true)
        {
            Debug.Write($"Building Asteroid : {0.0:000},");
            SConsole.Write($"Building Asteroid : {0.0:000},");

            Stopwatch timer = Stopwatch.StartNew();

            if (multiThread)
                ProcessAsteroidMultiThread(voxelMap, materialIndex, func, readWrite);
            else
                ProcessAsteroidSingleThread(voxelMap, materialIndex, func, readWrite);

            timer.Stop();

            voxelMap.RefreshAssets();

            MyVoxelMapBase.UpdateContentBounds(voxelMap);

            SConsole.WriteLine($" Done. | {timer.Elapsed}  | VoxCells {voxelMap.VoxCells:#,##0}");
        }

        static void ProcessAsteroidSingleThread(MyVoxelMapBase voxelMap, byte materialIndex, VoxelBuilderAction func, bool readWrite)
        {
            long counterTotal = (long)voxelMap.Size.X * voxelMap.Size.Y * voxelMap.Size.Z;
            long counter = 0;
            decimal progress = 0;

            const int cellSize = 64;

            Vector3I cacheSize = Vector3I.Min(new(cellSize), voxelMap.Storage.Size);
            MyStorageData oldCache = new();

            oldCache.Resize(cacheSize);

            Vector3I block = Vector3I.Zero;
            PRange.ProcessRange(block, cacheSize / cellSize);
            // LOD0 is required to read if you intend to write back to the voxel storage.
            Vector3I maxRange = block + cacheSize - 1;
            voxelMap.Storage.ReadRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, maxRange);

            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);
            byte volume = 0;
            byte cellMaterial = materialIndex;

            if (readWrite)
            {
                volume = oldCache.Content(ref p);
                cellMaterial = oldCache.Material(ref p);
            }

            var coords = block + p;
            var args = new MyVoxelBuilderArgs(voxelMap.Size, coords, cellMaterial, volume);

            func(ref args);

            if (args.Volume != volume)
                oldCache.Set(MyStorageDataTypeEnum.Content, ref p, args.Volume);

            if (args.MaterialIndex != cellMaterial)
                oldCache.Set(MyStorageDataTypeEnum.Material, ref p, args.MaterialIndex);


            voxelMap.Storage.WriteRange(oldCache, MyStorageDataTypeFlags.ContentAndMaterial, block, maxRange);

            counter += (long)cacheSize.X * cacheSize.Y * cacheSize.Z;

            decimal prog = Math.Floor(counter / (decimal)counterTotal * 100);

            if (prog != progress)
            {
                progress = prog;
                SConsole.Write($"{progress:000},");
            }
        }



        static void ProcessAsteroidMultiThread(MyVoxelMapBase voxelMap, byte materialIndex, VoxelBuilderAction func, bool readWrite)
        {
            long counterTotal = (long)voxelMap.Size.X * voxelMap.Size.Y * voxelMap.Size.Z;
            long counter = 0;
            decimal progress = 0;

            const int cellSize = 64;

            Vector3I cacheSize = Vector3I.Min(new(cellSize), voxelMap.Storage.Size);
            var block = Vector3I.Zero;
            PRange.ProcessRange(block, voxelMap.Storage.Size / cellSize);

            var cache = new MyStorageData();
            cache.Resize(cacheSize);
            Vector3I maxRange = block + cacheSize - 1;
            voxelMap.Storage.ReadRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, 0, block, maxRange);

            Vector3I p = Vector3I.Zero;
            PRange.ProcessRange(p, cacheSize);

            byte volume = 0;
            byte cellMaterial = materialIndex;

            if (readWrite)
            {
                volume = cache.Content(ref p);
                cellMaterial = cache.Material(ref p);
            }

            var coords = block + p;
            var args = new MyVoxelBuilderArgs(voxelMap.Size, coords, cellMaterial, volume);

            func(ref args);

            if (args.Volume != volume)
                cache.Set(MyStorageDataTypeEnum.Content, ref p, args.Volume);

            if (args.MaterialIndex != cellMaterial)
                cache.Set(MyStorageDataTypeEnum.Material, ref p, args.MaterialIndex);

            Interlocked.Add(ref counter, (long)cacheSize.X * cacheSize.Y * cacheSize.Z);

            lock (Locker)
            {

                voxelMap.Storage.WriteRange(cache, MyStorageDataTypeFlags.ContentAndMaterial, block, maxRange);
            }

            long c = Interlocked.Add(ref counter, (long)cacheSize.X * cacheSize.Y * cacheSize.Z);
            decimal prog = Math.Floor(c / (decimal)counterTotal * 100);

            if (prog > progress)
            {
                progress = prog;


                SConsole.Write($"{progress:000},");
            }


            GC.Collect();

        }

        #endregion

        private static Vector3I CalcRequiredSize(int size)
        {
            // the size of 4x4x4 is too small. the game allows it, but the physics is broken.
            // So I'm restricting the smallest to 8x8x8.
            // All voxels are cubic, and powers of 2 in size.
            return new Vector3I(MathHelper.GetNearestBiggerPowerOfTwo(Math.Max(8, size)));
        }

        public static Vector3I CalcRequiredSize(Vector3I size)
        {
            // the size of 4x4x4 is too small. the game allows it, but the physics is broken.
            // So I'm restricting the smallest to 8x8x8.
            // All voxels are cubic, and powers of 2 in size.
            return new Vector3I(MathHelper.GetNearestBiggerPowerOfTwo(SpaceEngineersExtensions.Max(8, size.X, size.Y, size.Z)));
        }
    }
}

