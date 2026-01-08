using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using SEToolbox.Interop;
using SEToolbox.Interop.Asteroids;
using SEToolbox.Models.Asteroids;
using SEToolbox.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VRageMath;
using MyVoxelMap = SEToolbox.Interop.Asteroids.MyVoxelMapBase;

namespace ToolboxTest
{
    [TestClass]
    public class VoxelTests
    {
        [TestInitialize]
        public void InitTest()
        {
            SpaceEngineersCore.LoadDefinitions();
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelCompressionV1()
        {
            const string fileOriginal = @".\TestAssets\asteroid_0_moon_4.vox";
            const string fileExtracted = @".\TestOutput\asteroid_0_moon_4.vox.bin";
            const string fileNew = @".\TestOutput\asteroid_0_moon_4_test.vox";
            MyVoxelMap.UncompressV1(fileOriginal, fileExtracted);
            MyVoxelMap.CompressV1(fileExtracted, fileNew);

            long lengthOriginal = new FileInfo(fileOriginal).Length;
            long lengthExtracted = new FileInfo(fileExtracted).Length;
            long lengthNew = new FileInfo(fileNew).Length;

            Assert.AreEqual(9428, lengthOriginal, "File size must match.");
            Assert.AreEqual(310276, lengthExtracted, "File size must match.");
            Assert.AreEqual(9428, lengthNew, "File size must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelMaterials()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;

            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelLoadSaveVox()
        {
            const string fileOriginal = @".\TestAssets\asteroid_0_moon_4.vox";
            const string fileNew = @".\TestOutput\asteroid_0_moon_4_save.vx2";

            MyVoxelMap.UpdateFileFormat(fileOriginal, fileNew);

            using MyVoxelMap voxelMap = new();

            voxelMap.Load(fileNew);
            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            long lengthOriginal = new FileInfo(fileOriginal).Length;
            long lengthNew = new FileInfo(fileNew).Length;

            Assert.AreEqual(9428, lengthOriginal, "File size must match.");
            Assert.AreEqual(9431, lengthNew, "File size must match.");
        }

        private Dictionary<string, long> assetNames = new()
        {
                {"Stone_04", 59954503},
                {"Iron_02", 55380508},
                {"Magnesium_01", 55380508},
                {"Platinum_01", 9756991},
                {"Gold_01", 3448256},
                {"Nickel_01", 60253},
                {"Uraninite_01", 1377765},
                {"Silver_01", 4602677},
                {"Cobalt_01", 2446253},
                {"Silicon_01", 38760}
        };
        [TestMethod, TestCategory("UnitTest")]
        public void VoxelLoadSaveVx2_V1()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            const string fileOriginal = @".\TestAssets\AsteroidFormat_V1.vx2";
            const string fileNew = @".\TestOutput\AsteroidFormat_V1_save.vx2";

            using MyVoxelMap voxelMap = new();
            voxelMap.Load(fileOriginal);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();


            Assert.AreEqual(147470221, voxelMap.VoxCells, "VoxCells count should be equal.");
            Assert.AreEqual(10, assetNameCount.Count, "Asset count should be equal.");

            foreach (var item in assetNames)
            {
                Assert.IsTrue(assetNameCount.ContainsKey(item.Key), $"{item.Key} asset should exist.");
                Assert.AreEqual(item.Value, assetNameCount[item.Key], $"{item.Key} count should be equal.");
            }

            voxelMap.Save(fileNew);

            long lengthOriginal = new FileInfo(fileOriginal).Length;
            long lengthNew = new FileInfo(fileNew).Length;

            Assert.AreEqual(88299, lengthOriginal, "File size must match.");
            Assert.AreEqual(134301, lengthNew, "File size must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelLoadSaveVx2_V2()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            const string fileOriginal = @".\TestAssets\AsteroidForma_V2.vx2";
            const string fileNew = @".\TestOutput\Asteroid_Format_V2_save.vx2";

            using MyVoxelMap voxelMap = new();
            voxelMap.Load(fileOriginal);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            Assert.AreEqual(147470221, voxelMap.VoxCells, "VoxCells count should be equal.");

            Assert.AreEqual(10, assetNameCount.Count, "Asset count should be equal.");

            foreach (var item in assetNames)
            {
                Assert.IsTrue(assetNameCount.ContainsKey(item.Key), $"{item.Key} asset should exist.");
                Assert.AreEqual(item.Value, assetNameCount[item.Key], $"{item.Key} count should be equal.");
            }

            voxelMap.Save(fileNew);

            long lengthOriginal = new FileInfo(fileOriginal).Length;
            long lengthNew = new FileInfo(fileNew).Length;

            Assert.AreEqual(72296, lengthOriginal, "File size must match.");
            Assert.AreEqual(134301, lengthNew, "File size must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelLoadSaveVx2_V3()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            const string fileOriginal = @".\TestAssets\AsteroidFormat_V3.vx2";
            const string fileNew = @".\TestOutput\AsteroidFormat_V3_save.vx2";

            using MyVoxelMap voxelMap = new();
            voxelMap.Load(fileOriginal);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            Assert.AreEqual(147470221, voxelMap.VoxCells, "VoxCells count should be equal.");

            Assert.AreEqual(10, assetNameCount.Count, "Asset count should be equal.");

            foreach (var item in assetNames)
            {
                Assert.IsTrue(assetNameCount.ContainsKey(item.Key), $"{item.Key} asset should exist.");
                Assert.AreEqual(item.Value, assetNameCount[item.Key], $"{item.Key} count should be equal.");
            }

            voxelMap.Save(fileNew);

            long lengthOriginal = new FileInfo(fileOriginal).Length;
            long lengthNew = new FileInfo(fileNew).Length;

            Assert.AreEqual(145351, lengthOriginal, "Original File size must match.");
            Assert.AreEqual(144997, lengthNew, "New File size must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelLoadStock()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            string redShipCrashedAsteroidPath = Path.Combine(contentPath, "VoxelMaps", "RedShipCrashedAsteroid.vx2");

            using MyVoxelMap voxelMap = new();
            voxelMap.Load(redShipCrashedAsteroidPath);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            Assert.AreEqual(139716285, voxelMap.VoxCells, "VoxCells count should be equal.");

            Assert.AreEqual(7, assetNameCount.Count, "Asset count should be equal.");
            Dictionary<string, long> assetNames = new()
            {
                { "Stone_04", 110319366 },
                { "Platinum_01", 17876016 }
            };


            if (!assetNameCount.ContainsKey("Stone_01"))
            {
                foreach (var item in assetNames)
                {
                    Assert.IsTrue(assetNameCount.ContainsKey(item.Key), $"{item.Key} asset should exist.");
                    Assert.AreEqual(item.Value, assetNameCount[item.Key], $"{item.Key} count should be equal.");
                }
            }

            long lengthOriginal = new FileInfo(redShipCrashedAsteroidPath).Length;
            Assert.AreEqual(109192, lengthOriginal, "File size must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelDetails()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            const string fileOriginal = @".\TestAssets\DeformedSphereWithHoles_64x128x64.vx2";

            using MyVoxelMap voxelMap = new();

            voxelMap.Load(fileOriginal);
            voxelMap.RefreshAssets();

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            Assert.AreEqual(128, voxelMap.Size.X, "Voxel Bounding size must match.");
            Assert.AreEqual(128, voxelMap.Size.Y, "Voxel Bounding size must match.");
            Assert.AreEqual(128, voxelMap.Size.Z, "Voxel Bounding size must match.");

            Assert.AreEqual(48, voxelMap.BoundingContent.Size.X + 1, "Voxel Content size must match.");
            Assert.AreEqual(112, voxelMap.BoundingContent.Size.Y + 1, "Voxel Content size must match.");
            Assert.AreEqual(48, voxelMap.BoundingContent.Size.Z + 1, "Voxel Content size must match.");

            Assert.AreEqual(30909925, voxelMap.VoxCells, "Voxel cells must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelMaterialIndexes()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            for (byte i = 0; i < materials.Count; i++)
            {
                Assert.AreEqual(i, SpaceEngineersResources.GetMaterialIndex(materials[i].Id.SubtypeName), "Material index should equal original.");
            }

            // Cannot test for non-existing material.
            Assert.AreEqual(0xFF, SpaceEngineersResources.GetMaterialIndex("blaggg"), "Material index should not exist.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelMaterialChanges()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            VRage.Game.MyVoxelMaterialDefinition stoneMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone"));
            Assert.IsNotNull(stoneMaterial, "Stone material should exist.");

            VRage.Game.MyVoxelMaterialDefinition goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            const string fileOriginal = @".\TestAssets\asteroid_0_moon4.vx2";
            const string fileNew = @".\TestOutput\asteroid_0_moon4_gold.vx2";

            MyVoxelBuilder.ConvertAsteroid(fileOriginal, fileNew, stoneMaterial.Id.SubtypeName, goldMaterial.Id.SubtypeName);

            long lengthOriginal = new FileInfo(fileOriginal).Length;
            long lengthNew = new FileInfo(fileNew).Length;

            Assert.AreEqual(9431, lengthOriginal, "Original file size must match.");
            Assert.AreEqual(14618, lengthNew, "New file size must match.");

            using MyVoxelMap voxelMapOriginal = new();
            voxelMapOriginal.Load(fileOriginal);

            Assert.IsTrue(voxelMapOriginal.IsValid, "Voxel format must be valid.");

            Dictionary<string, long> assetNameCountOriginal = voxelMapOriginal.RefreshAssets();


            Assert.AreEqual(10654637, voxelMapOriginal.VoxCells, "VoxCells count should be equal.");
            Assert.AreEqual(1, assetNameCountOriginal.Count, "Asset count should be equal.");
            Assert.IsTrue(assetNameCountOriginal.ContainsKey("Stone_05"), "Stone_05 asset should exist.");
            Assert.AreEqual(10654637, assetNameCountOriginal["Stone_05"], "Stone_05 count should be equal.");

            MyVoxelMap voxelMapNew = new();
            voxelMapNew.Load(fileNew);

            Assert.IsTrue(voxelMapNew.IsValid, "Voxel format must be valid.");

            Dictionary<string, long> assetNameCountNew = voxelMapNew.RefreshAssets();
            Assert.AreEqual(10654637, voxelMapNew.VoxCells, "VoxCells count should be equal.");
            Assert.AreEqual(1, assetNameCountNew.Count, "Asset count should be equal.");
            Assert.IsTrue(assetNameCountNew.ContainsKey("Gold_01"), "Gold_01 asset should exist.");
            Assert.AreEqual(10654637, assetNameCountNew["Gold_01"], "Gold_01 count should be equal.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelMaterialAssets_FixedSize()
        {
            const string fileOriginal = @".\TestAssets\test_cube_2x2x2.vx2";

            using MyVoxelMap voxelMap = new();
            voxelMap.Load(fileOriginal);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            Assert.AreEqual(2040, voxelMap.VoxCells, "VoxCells count should be equal.");
            Assert.AreEqual(1, assetNameCount.Count, "Asset count should be equal.");
            Assert.IsTrue(assetNameCount.ContainsKey("Stone_02"), "Stone_02 asset should exist.");
            Assert.AreEqual(2040, assetNameCount["Stone_02"], "Stone_02 count should be equal.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelMaterialAssets_FixedSize_MixedContent()
        {
            const string fileOriginal = @".\TestAssets\test_cube_2x2x2_mixed.vx2";

            using MyVoxelMap voxelMap = new();
            voxelMap.Load(fileOriginal);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            Assert.AreEqual(2040, voxelMap.VoxCells, "VoxCells count should be equal.");
            Assert.AreEqual(8, assetNameCount.Count, "Asset count should be equal.");
            List<string> assetNames =
            [
                "Iron_01",
                "Magnesium_01",
                "Platinum_01",
                "Uraninite_01",
                "Silver_01",
                "Gold_01",
                "Silicon_01",
                "Cobalt_01",
            ];
            foreach (var item in assetNames)
            {
                Assert.IsTrue(assetNameCount.ContainsKey(item), $"{item} asset should exist.");
                Assert.AreEqual(255, assetNameCount[item], $"{item} count should be equal.");
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelMaterialAssetsRandom()
        {
            Dictionary<string, string> materialNames = new()
            {
                {"Stone_05", "Stone" },
                {"Gold", "Gold" },
                {"Uraninite_01", "Uranium" },
            };
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");
            foreach (var item in materialNames)
            {
                VRage.Game.MyVoxelMaterialDefinition material = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains($"{item.Key}"));
                Assert.IsNotNull(material, $"{item.Value} material should exist.");
            }


            const string fileOriginal = @".\TestAssets\Arabian_Border_7.vx2";

            using MyVoxelMap voxelMap = new();
            voxelMap.Load(fileOriginal);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            IList<byte> materialAssets = voxelMap.CalcVoxelMaterialList();
            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            Assert.AreEqual(35465, materialAssets.Count, "Asset count should be equal.");

            Assert.AreEqual(8538122, voxelMap.VoxCells, "VoxCells count should be equal.");
            Assert.AreEqual(1, assetNameCount.Count, "Asset count should be equal.");

            Assert.IsTrue(assetNameCount.ContainsKey("Stone_01"), "Stone_01 asset should exist.");
            Assert.AreEqual(8538122, assetNameCount["Stone_01"], "Stone_01 count should be equal.");

            // Create matieral distribution of set percentages, with remainder to Stone.

            double[] distribution = [double.NaN, .5, .25];

            byte[] materialSelection = [
                SpaceEngineersResources.GetMaterialIndex(materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone_05")).Id.SubtypeName),
                SpaceEngineersResources.GetMaterialIndex(materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold")).Id.SubtypeName),
                SpaceEngineersResources.GetMaterialIndex(materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Uraninite_01")).Id.SubtypeName)
            ];

            List<byte> newMaterialAssetDistribution = [];

            int count;

            for (int i = 1; i < distribution.Length; i++)
            {
                count = (int)Math.Floor(distribution[i] * materialAssets.Count); // Round down.

                for (int j = 0; j < count; j++)
                {
                    newMaterialAssetDistribution.Add(materialSelection[i]);
                }
            }

            count = materialAssets.Count - newMaterialAssetDistribution.Count;

            for (int j = 0; j < count; j++)
            {
                newMaterialAssetDistribution.Add(materialSelection[0]);
            }

            // Randomize the distribution.
            newMaterialAssetDistribution.Shuffle();

            // Update the materials in the voxel.
            voxelMap.SetVoxelMaterialList(newMaterialAssetDistribution);
            assetNameCount = voxelMap.RefreshAssets();

            Assert.AreEqual(8538122, voxelMap.VoxCells, "VoxCells count should be equal.");
            Assert.AreEqual(3, assetNameCount.Count, "Asset count should be equal.");

            // due to randomization distribution acting a little differently depending on how the octree fills,
            // we need test for 1 percent less than target value.
            Dictionary<string, double> assets = new()
            {
                {"Stone_05", 0.24 },
                {"Gold_01", 0.49},
               { "Uraninite_01", 0.24 }
            };
            foreach (var item in assets)
            {
                Assert.IsTrue(assetNameCount.ContainsKey(item.Key), $"{item} asset should exist.");
                Assert.IsTrue(assetNameCount[item.Key] > item.Value * voxelMap.VoxCells, $"{item} count should be equal.");
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelMaterialAssetsGenerateFixed()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            string[] files = [@".\TestAssets\Arabian_Border_7.vx2", @".\TestAssets\cube_52x52x52.vx2"];

            foreach (string fileOriginal in files)
            {
                foreach (var material in materials)
                {
                    string fileNewVoxel = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(fileOriginal)),
                        Path.GetFileNameWithoutExtension(fileOriginal) + "_" + material.Id.SubtypeName + ".vx2").ToLower();

                    using MyVoxelMap voxelMap = new();
                    voxelMap.Load(fileOriginal);

                    IList<byte> materialAssets = voxelMap.CalcVoxelMaterialList();

                    double[] distribution = [double.NaN, .99,];
                    byte[] materialSelection = [0, SpaceEngineersResources.GetMaterialIndex(material.Id.SubtypeName)];

                    List<byte> newDistribution = [];

                    int count;

                    for (int i = 1; i < distribution.Length; i++)
                    {
                        count = (int)Math.Floor(distribution[i] * materialAssets.Count); // Round down.

                        for (int j = 0; j < count; j++)
                        {
                            newDistribution.Add(materialSelection[i]);
                        }
                    }

                    count = materialAssets.Count - newDistribution.Count;

                    for (int j = 0; j < count; j++)
                    {
                        newDistribution.Add(materialSelection[0]);
                    }

                    newDistribution.Shuffle();

                    voxelMap.SetVoxelMaterialList(newDistribution);
                    voxelMap.Save(fileNewVoxel);
                }
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelGenerateBoxSmall()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            var stoneMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone_02"));
            Assert.IsNotNull(stoneMaterial, "Stone material should exist.");

            var goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            const string fileNew = @".\TestOutput\test_cube_solid_8x8x8_gold_single.vx2";

            int size = 8;
            using MyVoxelMap voxelMap = MyVoxelBuilder.BuildAsteroidCube(false, size, size, size, goldMaterial.Index, stoneMaterial.Index, false, 0);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            voxelMap.Save(fileNew);

            long lengthNew = new FileInfo(fileNew).Length;
            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            Assert.AreEqual(984, lengthNew, "New file size must match.");

            for (int i = 0; i < voxelMap.Size.Length(); i++)
            {
                Assert.AreEqual(16, voxelMap.Size[i], "Voxel Bounding size must match.");
            }

            for (int i = 0; i < voxelMap.BoundingContent.Size.Length(); i++)
            {
                Assert.AreEqual(size, voxelMap.BoundingContent.Size[i] + 1, "Voxel Content size must match.");
            }

            // Centered in the middle of 1 and 8.   1234-(4.5)-5678
            double[] vectors = [voxelMap.ContentCenter.X, voxelMap.ContentCenter.Y, voxelMap.ContentCenter.Z];
            for (int i = 0; i < vectors.Length; i++)
            {
                Assert.AreEqual(4.5, vectors[i], "Voxel Center must match.");
            }


            long voxels = (long)size * size * size * 255;
            Assert.AreEqual(voxels, voxelMap.VoxCells, "VoxCells count should be equal.");
            Assert.AreEqual(1, assetNameCount.Count, "Asset count should be equal.");
            Assert.IsTrue(assetNameCount.ContainsKey("Gold_01"), "Gold_01 asset should exist.");
            Assert.AreEqual(voxels, assetNameCount["Gold_01"], "Gold_01 count should be equal.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelGenerateBoxSmallMultiThread()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            var stoneMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone_02"));
            Assert.IsNotNull(stoneMaterial, "Stone material should exist.");

            var goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            const string fileNew = @".\TestOutput\test_cube_solid_8x8x8_gold_multi.vx2";

            int size = 8;
            using MyVoxelMap voxelMap = MyVoxelBuilder.BuildAsteroidCube(true, size, size, size, goldMaterial.Index, stoneMaterial.Index, false, 0);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            voxelMap.Save(fileNew);

            long lengthNew = new FileInfo(fileNew).Length;
            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            Assert.AreEqual(909, lengthNew, "New file size must match.");

            for (int i = 0; i < voxelMap.Size.Length(); i++)
            {
                Assert.AreEqual(16, voxelMap.Size[i], "Voxel Bounding size must match.");
            }

            for (int i = 0; i < voxelMap.BoundingContent.Size.Length(); i++)
            {
                Assert.AreEqual(size, voxelMap.BoundingContent.Size[i] + 1, "Voxel Content size must match.");
            }

            // Centered in the middle of 1 and 8.   1234-(4.5)-5678
            double[] vectors = [voxelMap.ContentCenter.X, voxelMap.ContentCenter.Y, voxelMap.ContentCenter.Z];
            for (int i = 0; i < vectors.Length; i++)
            {
                Assert.AreEqual(4.5, vectors[i], "Voxel Center must match.");
            }

            long voxels = (long)size * size * size * 255;
            Assert.AreEqual(voxels, voxelMap.VoxCells, "VoxCells count should be equal.");
            Assert.AreEqual(1, assetNameCount.Count, "Asset count should be equal.");
            Assert.IsTrue(assetNameCount.ContainsKey("Gold_01"), "Gold_01 asset should exist.");
            Assert.AreEqual(voxels, assetNameCount["Gold_01"], "Gold_01 count should be equal.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelGenerateSphereSmall()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            var stoneMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone_02"));
            Assert.IsNotNull(stoneMaterial, "Stone material should exist.");

            var goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            const string fileNew = @".\TestOutput\test_sphere_solid_7_gold.vx2";

            using MyVoxelMap voxelMap = MyVoxelBuilder.BuildAsteroidSphere(false, 4, goldMaterial.Index, stoneMaterial.Index, false, 0);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            voxelMap.Save(fileNew);

            long lengthNew = new FileInfo(fileNew).Length;

            Assert.AreEqual(1337, lengthNew, "New file size must match.");

            for (int i = 0; i < voxelMap.Size.Length(); i++)
            {
                Assert.AreEqual(64, voxelMap.Size[i], "Voxel Bounding size must match.");
            }

            for (int i = 0; i < voxelMap.BoundingContent.Size.Length(); i++)
            {
                Assert.AreEqual(7, voxelMap.BoundingContent.Size[i] + 1, "Voxel Content size must match.");
            }

            // Centered in the middle of the 256x256x256 cell.
            double[] vectors = [voxelMap.ContentCenter.X, voxelMap.ContentCenter.Y, voxelMap.ContentCenter.Z];
            for (int i = 0; i < vectors.Length; i++)
            {
                Assert.AreEqual(32, vectors[i], "Voxel Center must match.");
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelGenerateShape()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            var stoneMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone_02"));
            Assert.IsNotNull(stoneMaterial, "Stone material should exist.");

            var goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            Vector3I size = new(128, 128, 128);
            Vector3I actualSize = MyVoxelBuilder.CalcRequiredSize(size);

            using MyVoxelMap voxelMap = new();
            voxelMap.Create(actualSize, stoneMaterial.Index);

            MyShapeSphere sphereShape1 = new()
            {
                Center = new Vector3D(64, 64, 64),
                Radius = 40
            };

            voxelMap.UpdateVoxelShape(MyVoxelBase.OperationType.Fill, sphereShape1, goldMaterial.Index);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            for (int i = 0; i < voxelMap.BoundingContent.Size.Length(); i++)
            {
                Assert.AreEqual(81, voxelMap.BoundingContent.Size[i] + 1, "Voxel Content size must match.");
            }

            // Centered in the middle of the 256x256x256 cell.
            double[] vectors = [voxelMap.ContentCenter.X, voxelMap.ContentCenter.Y, voxelMap.ContentCenter.Z];
            for (int i = 0; i < vectors.Length; i++)
            {
                Assert.AreEqual(64, vectors[i], "Voxel Center must match.");
            }

            Assert.AreEqual(1, assetNameCount.Count, "Asset count should be equal.");

            Assert.IsTrue(assetNameCount.ContainsKey(goldMaterial.Id.SubtypeName), "Gold asset should exist.");
            Assert.IsTrue(assetNameCount[goldMaterial.Id.SubtypeName] > 10000, "Gold count should be equal.");

            //const string fileNew = @".\TestOutput\test_sphere_solid_7_gold.vx2";
            //voxelMap.Save(fileNew);

            //long lengthNew = new FileInfo(fileNew).Length;

            MyShapeBox sphereBox = new()
            {
                Boundaries = new BoundingBoxD
                {
                    Min = new Vector3D(0, 0, 0),
                    Max = new Vector3D(127, 127, 127)
                }
            };

            voxelMap.UpdateVoxelShape(MyVoxelBase.OperationType.Cut, sphereBox, 0);
            Dictionary<string, long> assetNameCount2 = voxelMap.RefreshAssets();

            Assert.AreEqual(0, voxelMap.VoxCells, "Voxel cells must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelGenerateSphereLarge()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            var stoneMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone_02"));
            Assert.IsNotNull(stoneMaterial, "Stone material should exist.");

            var goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            const string fileNew = @".\TestOutput\test_sphere_solid_499_gold.vx2";

            using MyVoxelMap voxelMap = MyVoxelBuilder.BuildAsteroidSphere(true, 250, goldMaterial.Index, stoneMaterial.Index, false, 0);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            voxelMap.Save(fileNew);
            voxelMap.RefreshAssets();

            long lengthNew = new FileInfo(fileNew).Length;

            Assert.AreEqual(16689203471, voxelMap.VoxCells, "Voxel cells must match.");

            //Assert.AreEqual(2392621, lengthNew, "New file size must match.");

            for (int i = 0; i < voxelMap.Size.Length(); i++)
            {
                Assert.AreEqual(512, voxelMap.Size[i], "Voxel Bounding size must match.");
            }

            for (int i = 0; i < voxelMap.BoundingContent.Size.Length(); i++)
            {
                Assert.AreEqual(501, voxelMap.BoundingContent.Size[i] + 1, "Voxel Content size must match.");
            }

            // Centered in the middle of the 256x256x256 cell.
            double[] vectors = [voxelMap.ContentCenter.X, voxelMap.ContentCenter.Y, voxelMap.ContentCenter.Z];
            for (int i = 0; i < vectors.Length; i++)
            {
                Assert.AreEqual(256, vectors[i], "Voxel Center must match.");
            }
        }

        // This is ignored, because the test takes too long to run.
        [Ignore]
        [TestMethod]
        public void VoxelGenerateSpikeWall()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            const string fileNew = @".\TestOutput\test_spike_wall.vx2";

            Vector3I size = new(1024, 1024, 64);

            void CellAction(ref MyVoxelBuilderArgs e)
            {
                e.Volume = 0x00;

                if (e.CoordinatePoint.X > 0 && e.CoordinatePoint.Y > 0 && e.CoordinatePoint.Z > 0 &&
                     e.CoordinatePoint.X < size.X - 1 && e.CoordinatePoint.Y < size.Y - 1 && e.CoordinatePoint.Z < size.Z - 1 && 
                    (e.CoordinatePoint.Z == 5 && (e.CoordinatePoint.X % 2 == 0) && (e.CoordinatePoint.Y % 2 == 0) ||
                    (e.CoordinatePoint.Z == 6 && ((e.CoordinatePoint.X + 1) % 2 == 0) && ((e.CoordinatePoint.Y + 1) % 2 == 0))))
                {
                    e.Volume = 0x92;
                }
            }

            using MyVoxelMap voxelMap = MyVoxelBuilder.BuildAsteroid(true, size, materials[0].Index, null, CellAction);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            voxelMap.Save(fileNew);

            long lengthNew = new FileInfo(fileNew).Length;
            int[] ints = [1024, 1024, 64];
            // Multi threading does not produce a consistant volume across the cells in the voxel, so the actual file content can vary!!
            for (int i = 0; i < voxelMap.Size.Length(); i++)
            {
                Assert.AreEqual(ints[i], voxelMap.Size[i], "Voxel Bounding size must match.");
            }
            ints = [1022, 1022, 2];

            for (int i = 0; i < voxelMap.BoundingContent.Size.Length(); i++)
            {
                Assert.AreEqual(ints[i], voxelMap.BoundingContent.Size[i] + 1, "Voxel Content size must match.");
            }
            double[] doubles = [511.5, 511.5, 5.5];
            // Centered in the middle of the 256x256x256 cell.
            double[] vectors = [voxelMap.ContentCenter.X, voxelMap.ContentCenter.Y, voxelMap.ContentCenter.Z];
            for (int i = 0; i < vectors.Length; i++)
            {
                Assert.AreEqual(doubles[i], vectors[i], "Voxel Center must match.");
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelGenerateSpikeCube()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            string fileNew = @".\TestOutput\test_spike_cube256.vx2";

            int length = 256;
            int min = 4;
            int max = length - 4;

            Vector3I size = new(length, length, length);

            int[][] buildparams =
            [
                [min, 0],
                [min + 1, 1],
                [max, 0],
                [max - 1, -1]
            ];

            void CellAction(ref MyVoxelBuilderArgs e)
            {
                e.Volume = 0x00;

                
                if (e.CoordinatePoint.X >= min && e.CoordinatePoint.X <= max
                    && e.CoordinatePoint.Y >= min && e.CoordinatePoint.Y <= max
                    && e.CoordinatePoint.Z >= min && e.CoordinatePoint.Z <= max)
                {
                    
                    var point = e.CoordinatePoint;
                    foreach (int[] t in buildparams)
                    {
                        if ((point.X == t[0] && (point.Z + t[1]) % 2 == 0 && (point.Y + t[1]) % 2 == 0)
                            || (point.Y == t[0] && (point.X + t[1]) % 2 == 0 && (point.Z + t[1]) % 2 == 0)
                            || (point.Z == t[0] && (point.X + t[1]) % 2 == 0 && (point.Y + t[1]) % 2 == 0))
                        {
                            e.Volume = 0x92;
                        }
                    }
                }
            }

            using MyVoxelMap voxelMap = MyVoxelBuilder.BuildAsteroid(true, size, materials[0].Index, null, CellAction);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            voxelMap.Save(fileNew);

            long lengthNew = new FileInfo(fileNew).Length;

            // Multi threading does not produce a consistant volume across the cells in the voxel, so the actual file content can vary!!
            Assert.IsTrue(lengthNew > 44000, "New file size must match.");

            for (int i = 0; i < voxelMap.Size.Length(); i++)
            {
                Assert.AreEqual(256, voxelMap.Size[i], "Voxel Bounding size must match.");
            }
            for (int i = 0; i < voxelMap.BoundingContent.Size.Length(); i++)
            {
                Assert.AreEqual(249, voxelMap.BoundingContent.Size[i] + 1, "Voxel Content size must match.");
            }

            // Centered in the middle of the 256x256x256 cell.
            double[] vectors = [voxelMap.ContentCenter.X, voxelMap.ContentCenter.Y, voxelMap.ContentCenter.Z];
            for (int i = 0; i < vectors.Length; i++)
            {
                Assert.AreEqual(128, vectors[i], "Voxel Center must match.");
            }
        }



        [TestMethod, TestCategory("UnitTest")]
        public void Voxel3DImportStl()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            var stoneMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone_02"));
            Assert.IsNotNull(stoneMaterial, "Stone material should exist.");

            var goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            const string modelFile = @".\TestAssets\buddha_fixed_bottom.stl";
            const string voxelFile = @".\TestOutput\buddha_fixed_bottom.vx2";

            var transform = MeshHelper.TransformVector(new(0, 0, 0), 0, 0, 180);

            using MyVoxelMap voxelMap = MyVoxelBuilder.BuildAsteroidFromModel(true, modelFile, goldMaterial.Index,
                stoneMaterial.Index, true, stoneMaterial.Index, ModelTraceVoxel.ThinSmoothed, 0.766, transform);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            voxelMap.Save(voxelFile);

            int[] ints = [50, 46, 70];
            for (int i = 0; i < voxelMap.BoundingContent.Size.Length(); i++)
            {
                if (voxelMap.BoundingContent.Size[i] != ints[i])
                {
                    Assert.AreEqual(i, voxelMap.BoundingContent.Size[i] + 1, "Voxel Content size must match.");
                }
            }
            double[] doubles = [30.5, 28.5, 40.5];
            double[] vectors = [voxelMap.ContentCenter.X, voxelMap.ContentCenter.Y, voxelMap.ContentCenter.Z];
            for (int i = 0; i < vectors.Length; i++)
            {
                if (vectors[i] != doubles[i])
                {
                    Assert.AreEqual(doubles[i], vectors[i], "Voxel Center must match.");
                }
            }
            Assert.AreEqual(18710790, voxelMap.VoxCells, "Voxel cells must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void LoadAllVoxelFiles()
        {
            string[] files = Directory.GetFiles(Path.Combine(ToolboxUpdater.GetApplicationContentPath(), "VoxelMaps"), "*.vx2");

            foreach (var fileName in files)
            {
                string name = Path.GetFileName(fileName);

                Stopwatch watch = new();
                watch.Start();

                using MyVoxelMap voxelMap = new();
                voxelMap.Load(fileName);

                watch.Stop();

                Debug.WriteLine($"FileName:\t{name}.vx2");
                Debug.WriteLine($"Load Time:\t{watch.Elapsed}");
                Debug.WriteLine($"Valid:\t{voxelMap.IsValid}");
                Debug.WriteLine($"Bounding Size:\t{voxelMap.Size.X} x {voxelMap.Size.Y} x {voxelMap.Size.Z} blocks");
                Debug.WriteLine("");
            }
        }

        // This is ignored, because the functionality is not in use, and it's also broken.
        [Ignore]
        [TestMethod]
        public void SeedFillVoxelFile()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            var stoneMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Stone_01"));
            Assert.IsNotNull(stoneMaterial, "Stone material should exist.");

            var ironMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Iron_02"));
            Assert.IsNotNull(ironMaterial, "Iron material should exist.");

            VRage.Game.MyVoxelMaterialDefinition goldMaterial = materials.FirstOrDefault(m => m.Id.SubtypeName.Contains("Gold"));
            Assert.IsNotNull(goldMaterial, "Gold material should exist.");

            using MyVoxelMap voxelMap = MyVoxelBuilder.BuildAsteroidCube(false, 64, 64, 64, stoneMaterial.Index, stoneMaterial.Index, false, 0);
            //using var voxelMap = MyVoxelBuilder.BuildAsteroidSphere(true, 64, stoneMaterial.Id.SubtypeName, stoneMaterial.Id.SubtypeName, false, 0);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            AsteroidSeedFiller filler = new();
            AsteroidSeedFillProperties fillProperties = new()
            {
                MainMaterial = new SEToolbox.Models.MaterialSelectionModel { Value = stoneMaterial.Id.SubtypeName },
                FirstMaterial = new SEToolbox.Models.MaterialSelectionModel { Value = ironMaterial.Id.SubtypeName },
                FirstRadius = 3,
                FirstVeins = 2,
                SecondMaterial = new SEToolbox.Models.MaterialSelectionModel { Value = goldMaterial.Id.SubtypeName },
                SecondRadius = 1,
                SecondVeins = 1,
            };

            filler.FillAsteroid(voxelMap, fillProperties);

            Assert.AreEqual(128, voxelMap.Size.X, "Voxel Bounding size must match.");
            Assert.AreEqual(128, voxelMap.Size.Y, "Voxel Bounding size must match.");
            Assert.AreEqual(128, voxelMap.Size.Z, "Voxel Bounding size must match.");

            Assert.AreEqual(64, voxelMap.BoundingContent.Size.X + 1, "Voxel Content size must match.");
            Assert.AreEqual(64, voxelMap.BoundingContent.Size.Y + 1, "Voxel Content size must match.");
            Assert.AreEqual(64, voxelMap.BoundingContent.Size.Z + 1, "Voxel Content size must match.");

            Assert.AreEqual(66846720, voxelMap.VoxCells, "Voxel cells must match.");  // 255 * 64 * 64 * 64

            Dictionary<string, long> assetNameCount = voxelMap.RefreshAssets();

            // A cube should produce full voxcells, so all of them are 255.

            // TODO: This test will randomly fail (because the seeding is random), as the seed is not been properly applied to existing volumes. Some empty volumes are getting into the seed list.
            Assert.AreEqual(3, assetNameCount.Count, "Asset count should be equal.");

            Assert.IsTrue(assetNameCount.ContainsKey(stoneMaterial.Id.SubtypeName), "Stone asset should exist.");
            //Assert.AreEqual(255, assetNameCount[stoneMaterial.Id.SubtypeName], "Stone count should be equal.");

            Assert.IsTrue(assetNameCount.ContainsKey(ironMaterial.Id.SubtypeName), "Iron asset should exist.");
            //Assert.AreEqual(255, assetNameCount[ironMaterial.Id.SubtypeName], "Iron count should be equal.");

            Assert.IsTrue(assetNameCount.ContainsKey(goldMaterial.Id.SubtypeName), "Gold asset should exist.");
            //Assert.AreEqual(255, assetNameCount[goldMaterial.Id.SubtypeName], "Gold count should be equal.");


            // Seeder is too random to provide stable values.
            // Assert.AreEqual(236032, stoneAssets.Count, "Stone assets should equal.");
            //Assert.AreEqual(23040,  ironAssets.Count , "Iron assets should equal.");
            //Assert.AreEqual(3072,  goldAssets.Count, "Gold assets should equal.");

            // Strip the original material.
            // voxelMap.RemoveMaterial(stoneMaterial.Id.SubtypeName);
            //const string fileNew = @".\TestOutput\randomSeedMaterialCube.vx2";
            //voxelMap.Save(fileNew);
            // long lengthNew = new FileInfo(fileNew).Length;
        }

        [TestMethod, TestCategory("UnitTest")]
        public void FetchVoxelV2DetailPreview()
        {
            const string fileOriginal = @".\TestAssets\DeformedSphereWithHoles_64x128x64.vx2";

            Vector3I size = MyVoxelMap.LoadVoxelSize(fileOriginal);

            Assert.AreEqual(128, size.X, "Voxel Bounding size must match.");
            Assert.AreEqual(128, size.Y, "Voxel Bounding size must match.");
            Assert.AreEqual(128, size.Z, "Voxel Bounding size must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void FetchVoxelV1DetailPreview()
        {
            const string fileOriginal = @".\TestAssets\asteroid_0_moon4.vox";

            Vector3I size = MyVoxelMap.LoadVoxelSize(fileOriginal);

            Assert.AreEqual(64, size.X, "Voxel Bounding size must match.");
            Assert.AreEqual(64, size.Y, "Voxel Bounding size must match.");
            Assert.AreEqual(64, size.Z, "Voxel Bounding size must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VoxelMaterialAssets_FilledVolume()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            const string fileNew = @".\TestOutput\test_filledvolume.vx2";
            const int length = 64;

            Vector3I size = new(length, length, length);

            static void CellAction(ref MyVoxelBuilderArgs e) => e.Volume = 0xFF;

            using MyVoxelMap voxelMap = MyVoxelBuilder.BuildAsteroid(true, size, materials[06].Index, null, CellAction);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            voxelMap.Save(fileNew);
            voxelMap.RefreshAssets();

            long lengthNew = new FileInfo(fileNew).Length;

            Assert.AreEqual(437, lengthNew, "New file size must match.");

            Assert.AreEqual(66846720, voxelMap.VoxCells, "Voxel Cells must match."); // 255 * 64 * 64 * 64
            Assert.AreEqual(64, voxelMap.Size.X, "Voxel Bounding size must match.");
            Assert.AreEqual(64, voxelMap.Size.Y, "Voxel Bounding size must match.");
            Assert.AreEqual(64, voxelMap.Size.Z, "Voxel Bounding size must match.");
        }

        // This is ignored, because the test takes too long to run.
        [Ignore]
        [TestMethod]
        public void VoxelGenerateSpikeCubeLarge()
        {
            var materials = SpaceEngineersResources.VoxelMaterialDefinitions;
            Assert.IsTrue(materials.Count > 0, "Materials should exist. Has the developer got Space Engineers installed?");

            string fileNew = @".\TestOutput\test_spike_cube1024.vx2";

            int length = 1024;
            int min = 4;
            int max = length - 4;

            Vector3I size = new(length, length, length);

            int[][] buildparams = [
                [min, 0],
                [min + 1, 1],
                [max, 0],
                [max - 1, -1]
            ];

            void CellAction(ref MyVoxelBuilderArgs e)
            {
                e.Volume = 0x00;

                if (e.CoordinatePoint.X > 0 && e.CoordinatePoint.Y > 0 && e.CoordinatePoint.Z > 0
                    && e.CoordinatePoint.X < size.X - 1 && e.CoordinatePoint.Y < size.Y - 1 && e.CoordinatePoint.Z < size.Z - 1
                    && e.CoordinatePoint.X >= min && e.CoordinatePoint.Y >= min && e.CoordinatePoint.Z >= min
                    && e.CoordinatePoint.X <= max && e.CoordinatePoint.Y <= max && e.CoordinatePoint.Z <= max)
                {
                    int x = e.CoordinatePoint.X;
                    int y = e.CoordinatePoint.Y;
                    int z = e.CoordinatePoint.Z;

                    foreach (int[] t in buildparams)
                    {
                        if (((x + t[1]) % 2 == 0) && ((z + t[1]) % 2 == 0) && (x == t[0] || y == t[0] || z == t[0]))
                        {
                            e.Volume = 0x92;
                        }
                    }
                }
            }

            using MyVoxelMap voxelMap = MyVoxelBuilder.BuildAsteroid(true, size, materials[0].Index, null, CellAction);

            Assert.IsTrue(voxelMap.IsValid, "Voxel format must be valid.");

            voxelMap.Save(fileNew);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void LoadLoader()
        {
            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            string arabianBorder7AsteroidPath = Path.Combine(contentPath, "VoxelMaps", "Arabian_Border_7.vx2");

            VoxelMapLoader.Load(arabianBorder7AsteroidPath);
        }
    }
}
