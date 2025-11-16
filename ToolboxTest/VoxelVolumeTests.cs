using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media.Media3D;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Support;
using VRageMath;

namespace ToolboxTest
{
    [TestClass]
    public class VoxelVolumeTests
    {
        [TestInitialize]
        public void InitTest()
        {
            SpaceEngineersCore.LoadDefinitions();
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelConvertToVolumetricOdd()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;

            VRage.Game.MyVoxelMaterialDefinition goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            string modelFile = @".\TestAssets\Sphere_Gold.3ds";
            Size3D scale = new(5, 5, 5);
            Matrix3D rotateTransform = Matrix3D.Identity;
            TraceType traceType = TraceType.Odd;
            TraceCount traceCount = TraceCount.Trace5;
            TraceDirection traceDirection = TraceDirection.XYZ;

            string asteroidFile = @".\TestOutput\test_sphere_odd.vx2";

            Model3DGroup model = MeshHelper.Load(modelFile, ignoreErrors: true);
            MyVoxelRayTracer.Model info = new(model, scale, rotateTransform, goldMaterial.Index);

            MyVoxelMapBase voxelMap = MyVoxelRayTracer.GenerateVoxelMapFromModel(info, rotateTransform, traceType, traceCount, traceDirection,
                ResetProgress, IncrementProgress, CompleteProgress, default);
            voxelMap.Save(asteroidFile);

            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            Assert.IsTrue(File.Exists(asteroidFile), "Generated file must exist");

            long voxelFileLength = new FileInfo(asteroidFile).Length;
            Assert.AreEqual(13641, voxelFileLength, "File size must match.");
            Assert.AreEqual(new Vector3I(32, 32, 32), voxelMap.Size, "Voxel Bounding size must match.");
            Assert.AreEqual(new Vector3I(27, 27, 27), voxelMap.BoundingContent.Size + 1, "Voxel Content size must match.");
            Assert.AreEqual(new VRageMath.Vector3D(16, 16, 16), voxelMap.ContentCenter, "Voxel Content Center must match.");

            Assert.AreEqual(2035523, voxelMap.VoxCells, "Voxel cells must match.");

            Assert.AreEqual(1, assetNameCount.Count, "Asset count should be equal.");
            Assert.IsTrue(assetNameCount.ContainsKey(goldMaterial.Id.SubtypeName), $"{goldMaterial.Id.SubtypeName} asset should exist.");
            Assert.AreEqual(2035523, assetNameCount[goldMaterial.Id.SubtypeName], $"{goldMaterial.Id.SubtypeName} count should be equal.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelConvertToVolumetricEven()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;

            // Use anything except for stone for testing, as Stone is a default material, and it shouldn't show up in the test.
            VRage.Game.MyVoxelMaterialDefinition goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            string modelFile = @".\TestAssets\Sphere_Gold.3ds";
            Size3D scale = new(5, 5, 5);
            Matrix3D rotateTransform = Matrix3D.Identity;
            TraceType traceType = TraceType.Even;
            TraceCount traceCount = TraceCount.Trace5;
            TraceDirection traceDirection = TraceDirection.XYZ;
            string asteroidFile = @".\TestOutput\test_sphere_even.vx2";

            Model3DGroup model = MeshHelper.Load(modelFile, ignoreErrors: true);
            MyVoxelRayTracer.Model info = new(model, scale, rotateTransform, goldMaterial.Index);

            MyVoxelMapBase voxelMap = MyVoxelRayTracer.GenerateVoxelMapFromModel(info, rotateTransform, traceType, traceCount, traceDirection,
                ResetProgress, IncrementProgress, CompleteProgress, default);
            voxelMap.Save(asteroidFile);

            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            Assert.IsTrue(File.Exists(asteroidFile), "Generated file must exist");

            long voxelFileLength = new FileInfo(asteroidFile).Length;
            Assert.AreEqual(13691, voxelFileLength, "File size must match.");
            Assert.AreEqual(new Vector3I(32, 32, 32), voxelMap.Size, "Voxel Bounding size must match.");
            Assert.AreEqual(new Vector3I(26, 26, 26), voxelMap.BoundingContent.Size + 1, "Voxel Content size must match.");
            Assert.AreEqual(new VRageMath.Vector3D(15.5, 15.5, 15.5), voxelMap.ContentCenter, "Voxel Content Center must match.");

            Assert.AreEqual(2047046, voxelMap.VoxCells, "Voxel cells must match.");

            Assert.AreEqual(1, assetNameCount.Count, "Asset count should be equal.");
            Assert.IsTrue(assetNameCount.ContainsKey(goldMaterial.Id.SubtypeName), $"{goldMaterial.Id.SubtypeName} asset should exist.");
            Assert.AreEqual(2047046, assetNameCount[goldMaterial.Id.SubtypeName], $"{goldMaterial.Id.SubtypeName} count should be equal.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelConvertToVolumetricCancel()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;

            VRage.Game.MyVoxelMaterialDefinition stoneMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone"));
            Assert.IsNotNull(stoneMaterial, "Stone material should exist.");

            string modelFile = @".\TestAssets\Sphere_Gold.3ds";
            Size3D scale = new(50, 50, 50);
            Matrix3D rotateTransform = Matrix3D.Identity;
            TraceType traceType = TraceType.Odd;
            TraceCount traceCount = TraceCount.Trace5;
            TraceDirection traceDirection = TraceDirection.XYZ;

            Model3DGroup model = MeshHelper.Load(modelFile, ignoreErrors: true);
            CancellationTokenSource cts = new();

            // cancel the convertion after 2 seconds.
            System.Timers.Timer timer = new(2000);
            timer.Elapsed += delegate
            {
                SConsole.WriteLine("Cancelling!!!");
                cts.Cancel();
                timer.Stop();
            };
            timer.Start();

            MyVoxelRayTracer.Model info = new(model, scale, rotateTransform, stoneMaterial.Index);
            MyVoxelMapBase voxelMap = MyVoxelRayTracer.GenerateVoxelMapFromModel(info, rotateTransform, traceType, traceCount, traceDirection,
                ResetProgress, IncrementProgress, CompleteProgress, cts.Token);

            Assert.IsNull(voxelMap, "Asteroid must not exist.");
        }

        // This was set up to test various things, including memory usage in x86. It's no longer required, but still a good test base.
        [Ignore]
        [TestMethod]
        public void VoxelConvertToVolumetricMisc()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;

            VRage.Game.MyVoxelMaterialDefinition stoneMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone"));
            Assert.IsNotNull(stoneMaterial, "Stone material should exist.");

            VRage.Game.MyVoxelMaterialDefinition goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            VRage.Game.MyVoxelMaterialDefinition silverMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Silver"));
            Assert.IsNotNull(silverMaterial, "Silver material should exist.");

            // Basic test...
            string modelFile = @".\TestAssets\Sphere_Gold.3ds";
            Size3D scale = new(5, 5, 5);
            Matrix3D rotateTransform = Matrix3D.Identity;
            TraceType traceType = TraceType.Odd;
            TraceCount traceCount = TraceCount.Trace5;
            TraceDirection traceDirection = TraceDirection.XYZ;

            // Basic model test...
            //var modelFile = @".\TestAssets\TwoSpheres.3ds";
            //var scale = new Size3D(5, 5, 5);

            // Scale test...
            //var modelFile = @".\TestAssets\Sphere_Gold.3ds";
            //var scale = new Size3D(20, 20, 20);
            //var rotateTransform = Matrix3D.Identity;

            // Max Scale test...  will cause an OutOfMemory exception at this scale because MSTest runs in x86.
            //var modelFile = @".\TestAssets\Sphere_Gold.3ds";
            //var scale = new Size3D(120, 120, 120);
            //var rotateTransform = Matrix3D.Identity;

            // Memory test (probably won't load in Space Engineers) ...  will cause an OutOfMemory exception at this scale because MSTest runs in x86.
            //var modelFile = @".\TestAssets\Sphere_Gold.3ds";
            //var scale = new Size3D(200, 200, 200);

            // Complexity test...
            //var modelFile = @".\TestAssets\buddha_fixed_bottom.stl";
            //var scale = new Size3D(0.78, 0.78, 0.78);
            //var rotateTransform = MeshHelper.TransformVector(new Vector3D(0, 0, 0), 180, 0, 0).Value;

            string[] modelMaterials = [stoneMaterial.Id.SubtypeName, goldMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName];
            string fillerMaterial = silverMaterial.Id.SubtypeName;
            string asteroidFile = @".\TestOutput\test_sphere.vx2";

            Model3DGroup model = MeshHelper.Load(modelFile, ignoreErrors: true);
            MyVoxelRayTracer.Model info = new(model, scale, rotateTransform, stoneMaterial.Index);

            MyVoxelMapBase voxelMap = MyVoxelRayTracer.GenerateVoxelMapFromModel(info, rotateTransform, traceType, traceCount, traceDirection,
                ResetProgress, IncrementProgress, CompleteProgress, default);
            voxelMap.Save(asteroidFile);

            Assert.IsTrue(File.Exists(asteroidFile), "Generated file must exist");

            long voxelFileLength = new FileInfo(asteroidFile).Length;

            Assert.IsTrue(voxelFileLength > 0, "File must not be empty.");

            Assert.IsTrue(voxelMap.Size.X > 0, "Voxel Size must be greater than zero.");
            Assert.IsTrue(voxelMap.Size.Y > 0, "Voxel Size must be greater than zero.");
            Assert.IsTrue(voxelMap.Size.Z > 0, "Voxel Size must be greater than zero.");

            Assert.IsTrue(voxelMap.BoundingContent.Size.X > 0, "Voxel ContentSize must be greater than zero.");
            Assert.IsTrue(voxelMap.BoundingContent.Size.Y > 0, "Voxel ContentSize must be greater than zero.");
            Assert.IsTrue(voxelMap.BoundingContent.Size.Z > 0, "Voxel ContentSize must be greater than zero.");

            Assert.IsTrue(voxelMap.VoxCells > 0, "voxCells must be greater than zero.");
        }

        #region Helpers

        private static double _counter;
        private static double _maximumProgress;
        private static int _percent;
        private static Stopwatch _timer;

        public static void ResetProgress(double initial, double maximumProgress)
        {
            _percent = 0;
            _counter = initial;
            _maximumProgress = maximumProgress;
            _timer = new Stopwatch();
            _timer.Start();

            SConsole.WriteLine($"{_percent}%  {_counter:#,##0}/{_maximumProgress:#,##0}  {_timer.Elapsed}/Estimating");
        }

        public static void IncrementProgress()
        {
            _counter++;

            int p = (int)(_counter / _maximumProgress * 100);

            if (_percent < p)
            {
                TimeSpan elapsed = _timer.Elapsed;
                TimeSpan estimate = new(p == 0 ? 0 : (long)((double)elapsed.Ticks / ((double)p / 100f)));
                _percent = p;

                SConsole.WriteLine($"{_percent}%  {_counter:#,##0}/{_maximumProgress:#,##0}  {elapsed}/{estimate}");
            }
        }

        public static void CompleteProgress()
        {
            SConsole.WriteLine("Complete Step finished.");
        }

        #endregion
    }
}
