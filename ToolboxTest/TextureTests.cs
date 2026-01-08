
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using SEToolbox.Interop;
using SEToolbox.Support;
using VRage.Game;
using VRage.ObjectBuilders;
using Brushes = System.Drawing.Brushes;
using Effects = SEToolbox.ImageLibrary.Effects;
using MOBTypeIds = SEToolbox.Interop.SpaceEngineersTypes.MOBTypeIds;
using TexUtil = SEToolbox.ImageLibrary.ImageTextureUtil;

namespace ToolboxTest
{
    [TestClass]
    public class TextureTests
    {
        private string _path;

        [TestInitialize]
        public void InitTest()
        {
            SpaceEngineersCore.LoadDefinitions();
            _path = Path.GetFullPath(".\\TestOutput");

            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("DX9")]
        public void LoadComponentTextures()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\Models\Cubes\DoorBlock_cm.dds"));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\GUI\Icons\Cubes\ExplosivesComponent.dds"));

            Dictionary<MyObjectBuilderType, (string, Type)> mobTypes = new()
            {
                { MOBTypeIds.AmmoMagazine,( "Magnesium",typeof(MyPhysicalItemDefinition))},
                { MOBTypeIds.Ingot, ("Gold",typeof(MyPhysicalItemDefinition))},
                { MOBTypeIds.AmmoMagazine, ("NATO_5p56x45mm",typeof(MyAmmoMagazineDefinition))},
                { MOBTypeIds.Component, ("Steel Plate", typeof(MyComponentDefinition))},
                { MOBTypeIds.LandingGear, ("SmallBlockLandingGear", typeof(MyLandingGearDefinition))},
            };

            var definitions = MyDefinitionManager.Static.GetDefinitions<MyDefinitionBase>().First(e => e.Id.TypeId == mobTypes.Keys.First());
            foreach (var type in mobTypes)
            {
                MyDefinitionBase def = MyDefinitionManager.Static.GetDefinition(type.Key, type.Value.Item1);
                Assert.AreEqual(def.GetType(), type.Value.Item2);
            }
            string path = Path.Combine(contentPath, definitions.Icons.First());
            TestLoadTextureAndExport(Path.Combine(contentPath, definitions.Icons.First()));

            var iconHash = new HashSet<string>(
            [
                @"Textures\GUI\Controls\grid_item.dds",
                @"Textures\BackgroundCube\Prerender\Sun.dds",
                @"Textures\Voxels\Gold_01_ForAxisXZ_cm.dds",
                @"Textures\Voxels\Silicon_01_ForAxisXZ_cm.dds",
                @"Textures\Voxels\Platinum_01_ForAxisXZ_cm.dds",
                @"Textures\Models\Characters\Astronaut\Astronaut_cm.dds",
                @"Textures\Models\Characters\Astronaut\Astronaut_ng.dds"
            ]);

            foreach (var icon in iconHash)
            {
                var isAstronaut = icon.EndsWith("Astronaut_cm.dds" ?? "Astronaut_ng.dds");
                TestLoadTextureAndExport(Path.Combine(contentPath, icon), isAstronaut);
            }
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("DX10")]
        public void LoadComponentTexturesDx10PremultipliedAlpha()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");
            string contentPath = ToolboxUpdater.GetApplicationContentPath();
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\GUI\Icons\Cubes\AdvancedMotor.dds"));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\GUI\Icons\component\ExplosivesComponent.dds"));
        }

        [TestMethod, TestCategory("UnitTest"), TestCategory("DX11")]
        public void LoadComponentTexturesDx11()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\Models\Cubes\large_medical_room_cm.dds"));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\Models\Cubes\large_medical_room_cm.dds"), true);
        }

        private static Bitmap TestLoadTexture(string textureFilePath, int depthSlice = 0, int width = -1, int height = -1, bool ignoreAlpha = false)
        {
            Assert.IsTrue(File.Exists(textureFilePath), $"Filepath {textureFilePath} should exist on developer machine.");

            string name = Path.GetFileNameWithoutExtension(textureFilePath) + (ignoreAlpha ? "_alpha" : "");
            Bitmap textureFilePathBmp;
            using FileStream stream = File.OpenRead(textureFilePath);
            textureFilePathBmp = TexUtil.CreateBitmap(stream, textureFilePath, depthSlice, width, height, ignoreAlpha);

            Assert.IsNotNull(textureFilePathBmp, $"Texture for {name} should not be null.");

            return textureFilePathBmp;
        }


        private void TestLoadTextureAndExport(string textureFilePath, bool ignoreAlpha = false)
        {
            Assert.IsTrue(File.Exists(textureFilePath), $"Filepath {textureFilePath} should exist on developer machine.");

            string name = Path.GetFileNameWithoutExtension(textureFilePath) + (ignoreAlpha ? "_alpha" : "");
            Bitmap textureFilePathBmp;
            using FileStream stream = File.OpenRead(textureFilePath);
            textureFilePathBmp = TexUtil.CreateBitmap(stream, textureFilePath, ignoreAlpha: ignoreAlpha);
            Assert.IsNotNull(textureFilePathBmp, $"Texture for {name} should not be null.");

            File.Copy(textureFilePath, Path.Combine(_path, name + ".dds"), true);
            TexUtil.WriteImage(textureFilePathBmp, Path.Combine(_path, name + ".png"));
        }

        [TestMethod, TestCategory("DX10")]
        public void PixelEffectTextures()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            // ----

            Dictionary<string, string> effects = new()
            {
                {@"Cubes\large_medical_room_cm", "large_medical_room_cm"},
                {@"Cubes\large_thrust_large_cm", "large_thrust_large_me"},
                {@"Characters\Astronaut\Astronaut_cm", "Astronaut_me"},
                {@"Characters\Astronaut\Astronaut_ng", "Astronaut_ng"},
            };
            
            foreach (var item in effects)
            {
                string path = Path.Combine(contentPath, $@"Textures\Models\{item.Key}.dds");
                Assert.IsTrue(File.Exists(path), $"Filepath should exist on developer machine: {path}");
                if (item.Key != @"Characters\Astronaut\Astronaut_ng")
                {
                    Bitmap bmp = TestLoadTexture(path);
                    TexUtil.WriteImage(bmp, $@".\TestOutput\{item.Value}.png");
                    Bitmap bmp2 = TestLoadTexture(path, ignoreAlpha: true);
                    TexUtil.WriteImage(bmp2, $@".\TestOutput\{item.Value}_full.png");

                    Effects.IPixelEffect pixelEffect = new Effects.AlphaPixelEffect();
                    Bitmap alphaBmp = pixelEffect.Quantize(bmp);
                    TexUtil.WriteImage(alphaBmp, $@".\TestOutput\{item.Value}_alpha.png");
                    byte emIntensity = item.Key == @"Characters\Astronaut\Astronaut_cm" ? (byte)255 : (byte)0;

                    pixelEffect = new Effects.EmissivePixelEffect(emIntensity);
                    Bitmap emissiveBmp = pixelEffect.Quantize(bmp);
                    TexUtil.WriteImage(emissiveBmp, $@".\TestOutput\{item.Value}_emissive.png");
                    if (item.Key == @"Cubes\large_medical_room_cm")
                    {
                        _ = TexUtil.CreateImage(@"Textures\Models\Cubes\large_medical_room_cm.dds", false, new Effects.EmissivePixelEffect(emIntensity));
                    }
                }
                else
                {
                    string path2 = Path.Combine(contentPath, @"Textures\Models\Characters\Astronaut\Astronaut_ng.dds");
                    Assert.IsTrue(File.Exists(path2), "Filepath should exist on developer machine");
                    Bitmap bmpNg = TestLoadTexture(path2);
                    TexUtil.WriteImage(bmpNg, @".\TestOutput\Astronaut_ng.png");
                }
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void CreateMenuTextures()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            MyCubeBlockDefinition smallBlockLandingGear = (MyCubeBlockDefinition)MyDefinitionManager.Static.GetDefinition(new MyObjectBuilderType(typeof(MyObjectBuilder_LandingGear)), "SmallBlockLandingGear");
            string smallBlockLandingGearPath = Path.Combine(contentPath, smallBlockLandingGear.Icons.First());
            Assert.IsTrue(File.Exists(smallBlockLandingGearPath), "Filepath should exist on developer machine");
            Assert.IsNotNull(smallBlockLandingGear, "Type should match");
            Bitmap smallBlockLandingGearBmp = TestLoadTexture(smallBlockLandingGearPath);

            string gridItemPath = Path.Combine(contentPath, @"Textures\GUI\Controls\grid_item.dds");
            Assert.IsTrue(File.Exists(gridItemPath), "Filepath should exist on developer machine");
            Bitmap gridBmp = TestLoadTexture(gridItemPath);

            Bitmap bmp = TexUtil.MergeImages(gridBmp, smallBlockLandingGearBmp, Brushes.Black);
            TexUtil.WriteImage(bmp, @".\TestOutput\Menu_SmallBlockLandingGear.png");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void ReadBackgroundTextures()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            string backgroundPath = Path.Combine(contentPath, @"Textures\BackgroundCube\Final\BackgroundCube.dds");
            Assert.IsTrue(File.Exists(backgroundPath), "Filepath should exist on developer machine");
            Dictionary<string, int> testSizes = new()
            {
                { "Full", -1},
                { "1024", 1024},
                { "512", 512 },
                //{ "256", 256 },
                { "128", 128 },
                { "64", 64 },
                { "32", 32 },
            };

            foreach (KeyValuePair<string, int> kvp in testSizes)
            {
                int index = testSizes.Count -1;
                int size = kvp.Value;
                Bitmap backgroundBmp = TestLoadTexture(backgroundPath, index, size, size);
                TexUtil.WriteImage(backgroundBmp, $@".\TestOutput\BackgroundCube{index}_{size}.png");
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void CreateBackgroundPreview()
        {
            const int size = 128;
            const bool ignoreAlpha = true;

            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            string backgroundPath = Path.Combine(contentPath, @"Textures\BackgroundCube\Final\BackgroundCube.dds");
            Assert.IsTrue(File.Exists(backgroundPath), "Filepath should exist on developer machine");

            Bitmap result = new(size * 4, size * 3);

            using Graphics graphics = Graphics.FromImage(result);

            //set the resize quality modes to high quality
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

            Dictionary<int, int> alfaSets = new()
            {
                { size * 2, size * 1 },
                { size * 0, size * 1 },
                { size * 1, size * 0 },
                { size * 1, size * 2 },
                { size * 1, size * 1 },
                { size * 3, size * 1 },
            };

            if (ignoreAlpha)
            {
                foreach (var set in alfaSets)
                {
                    graphics.FillRectangle(Brushes.Black, set.Key, set.Value, size, size);
                }
            }

            foreach (var set in alfaSets)
            {
                graphics.DrawImage(TestLoadTexture(backgroundPath, alfaSets.Count, size, size, ignoreAlpha), set.Key, set.Value, size, size);
            }
            // Approximate position of local Sun and light source.
            graphics.FillEllipse(Brushes.White, size * 1 + (int)(size * 0.7), size * 2 + (int)(size * 0.93), (int)(size * 0.06), (int)(size * 0.06));

            TexUtil.WriteImage(result, string.Format($@".\TestOutput\BackgroundCube_{size}.png"));
        }

        // this is ignored, as it really isn't a unit test. It simply extracts game textures.
        [Ignore]
        [TestMethod]
        public void LoadAllCubeTextures()
        {
            string[] files = Directory.GetFiles(Path.Combine(ToolboxUpdater.GetApplicationContentPath(), @"Textures\Models\Cubes"), "*.dds");

            foreach (var fileName in files)
            {
                string outputFileName = Path.Combine(@".\TestOutput", Path.GetFileNameWithoutExtension(fileName) + ".png");
                Bitmap imageBitmap = TestLoadTexture(fileName);
                TexUtil.WriteImage(imageBitmap, outputFileName);
            }
        }
    }
}
