using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using SEToolbox.Interop;
using SEToolbox.Support;
using VRage.ObjectBuilders;
using Brushes = System.Drawing.Brushes;
using TexUtil = SEToolbox.ImageLibrary.ImageTextureUtil;
using Effects = SEToolbox.ImageLibrary.Effects;
using MOBTypeIds = SEToolbox.Interop.SpaceEngineersTypes.MOBTypeIds;
using VRage.Game;

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
                Directory.CreateDirectory(_path);
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


            var magnesiumOre = MyDefinitionManager.Static.GetDefinition(MOBTypeIds.Ore, "Magnesium");
            Assert.IsTrue(magnesiumOre is MyPhysicalItemDefinition, "Type should match");
            TestLoadTextureAndExport(Path.Combine(contentPath, magnesiumOre.Icons.First()));


            var goldIngot = MyDefinitionManager.Static.GetDefinition(MOBTypeIds.Ingot, "Gold");
            Assert.IsTrue(goldIngot is MyPhysicalItemDefinition, "Type should match");
            TestLoadTextureAndExport(Path.Combine(contentPath, goldIngot.Icons.First()));


            var ammoMagazine = MyDefinitionManager.Static.GetDefinition(MOBTypeIds.AmmoMagazine, "NATO_5p56x45mm");
            Assert.IsTrue(ammoMagazine is MyAmmoMagazineDefinition, "Type should match");
            TestLoadTextureAndExport(Path.Combine(contentPath, ammoMagazine.Icons.First()));


            var steelPlate = MyDefinitionManager.Static.GetDefinition(MOBTypeIds.Component, "SteelPlate");
            Assert.IsTrue(steelPlate is MyComponentDefinition, "Type should match");
            TestLoadTextureAndExport(Path.Combine(contentPath, steelPlate.Icons.First()));


            var smallBlockLandingGear = MyDefinitionManager.Static.GetDefinition(MOBTypeIds.LandingGear, "SmallBlockLandingGear");
            Assert.IsTrue(smallBlockLandingGear is MyCubeBlockDefinition, "Type should match");
            TestLoadTextureAndExport(Path.Combine(contentPath, smallBlockLandingGear.Icons.First()));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\GUI\Controls\grid_item.dds"));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\BackgroundCube\Prerender\Sun.dds"));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\Voxels\Gold_01_ForAxisXZ_cm.dds"));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\Voxels\Silicon_01_ForAxisXZ_cm.dds"));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\Voxels\Platinum_01_ForAxisXZ_cm.dds"));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\Models\Characters\Astronaut\Astronaut_cm.dds"));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\Models\Characters\Astronaut\Astronaut_cm.dds"), true);
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\Models\Characters\Astronaut\Astronaut_ng.dds"));
            TestLoadTextureAndExport(Path.Combine(contentPath, @"Textures\Models\Characters\Astronaut\Astronaut_ng.dds"), true);
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
            using var stream = File.OpenRead(textureFilePath);
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

            string medicalMetallicPath = Path.Combine(contentPath, @"Textures\Models\Cubes\large_medical_room_cm.dds"); // "32bpp RGBA"
            Assert.IsTrue(File.Exists(medicalMetallicPath), "Filepath should exist on developer machine");
            Bitmap medicalMetallicBmp = TestLoadTexture(medicalMetallicPath);
            TexUtil.WriteImage(medicalMetallicBmp, @".\TestOutput\large_medical_room_cm.png");

            Bitmap medicalMetallicBmp2 = TestLoadTexture(medicalMetallicPath, ignoreAlpha: true);
            TexUtil.WriteImage(medicalMetallicBmp2, @".\TestOutput\large_medical_room_cm_full.png");

            Effects.IPixelEffect effect = new Effects.AlphaPixelEffect();
            Bitmap medicalMetallicAlphaBmp = effect.Quantize(medicalMetallicBmp);
            TexUtil.WriteImage(medicalMetallicAlphaBmp, @".\TestOutput\large_medical_room_cm_alpha.png");

            effect = new Effects.EmissivePixelEffect(0);
            Bitmap medicalNormalSpecularEmissiveBmp = effect.Quantize(medicalMetallicBmp);
            TexUtil.WriteImage(medicalNormalSpecularEmissiveBmp, @".\TestOutput\large_medical_room_emissive.png");

            _ = TexUtil.CreateImage(medicalMetallicPath, false, new Effects.EmissivePixelEffect(0));

            // ----

            string largeThrustMetallicPath = Path.Combine(contentPath, @"Textures\Models\Cubes\large_thrust_large_cm.dds"); // "32bpp RGBA"
            Assert.IsTrue(File.Exists(largeThrustMetallicPath), "Filepath should exist on developer machine");
            Bitmap largeThrustMetallicBmp = TestLoadTexture(largeThrustMetallicPath);
            TexUtil.WriteImage(largeThrustMetallicBmp, @".\TestOutput\large_thrust_large_me.png");

            Bitmap largeThrustMetallicBmp2 = TestLoadTexture(largeThrustMetallicPath, ignoreAlpha: true);
            TexUtil.WriteImage(largeThrustMetallicBmp2, @".\TestOutput\large_thrust_large_me_full.png");

            effect = new Effects.AlphaPixelEffect();
            Bitmap largeThrustMetallicAlphaBmp = effect.Quantize(largeThrustMetallicBmp);
            TexUtil.WriteImage(largeThrustMetallicAlphaBmp, @".\TestOutput\large_thrust_large_me_alpha.png");

            effect = new Effects.EmissivePixelEffect(0);
            Bitmap largeThrustNormalSpecularEmissiveBmp = effect.Quantize(largeThrustMetallicBmp);
            TexUtil.WriteImage(largeThrustNormalSpecularEmissiveBmp, @".\TestOutput\large_thrust_large_me_emissive.png");

            // ----

            string astronautMaskEmissivePath = Path.Combine(contentPath, @"Textures\Models\Characters\Astronaut\Astronaut_cm.dds");
            Assert.IsTrue(File.Exists(astronautMaskEmissivePath), "Filepath should exist on developer machine");
            Bitmap astronautMaskEmissiveBmp = TestLoadTexture(astronautMaskEmissivePath);
            TexUtil.WriteImage(astronautMaskEmissiveBmp, @".\TestOutput\Astronaut_cm.png");

            effect = new Effects.AlphaPixelEffect();
            Bitmap astronautMaskEmissiveAlphaBmp = effect.Quantize(astronautMaskEmissiveBmp);
            TexUtil.WriteImage(astronautMaskEmissiveAlphaBmp, @".\TestOutput\Astronaut_me_alpha.png");

            Bitmap astronautMaskEmissiveBmp2 = TestLoadTexture(astronautMaskEmissivePath, ignoreAlpha: true);
            TexUtil.WriteImage(astronautMaskEmissiveBmp2, @".\TestOutput\Astronaut_me_full.png");

            effect = new Effects.EmissivePixelEffect(255);
            Bitmap astronautNormalSpecularEmissiveBmp = effect.Quantize(astronautMaskEmissiveBmp);
            TexUtil.WriteImage(astronautNormalSpecularEmissiveBmp, @".\TestOutput\Astronaut_me_emissive.png");

            // ----

            string astronautNormalSpecularPath = Path.Combine(contentPath, @"Textures\Models\Characters\Astronaut\Astronaut_ng.dds");
            Assert.IsTrue(File.Exists(astronautNormalSpecularPath), "Filepath should exist on developer machine");
            Bitmap astronautNormalSpecularBmp = TestLoadTexture(astronautNormalSpecularPath);
            TexUtil.WriteImage(astronautNormalSpecularBmp, @".\TestOutput\Astronaut_ng.png");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void CreateMenuTextures()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            var smallBlockLandingGear = (MyCubeBlockDefinition)MyDefinitionManager.Static.GetDefinition(new MyObjectBuilderType(typeof(MyObjectBuilder_LandingGear)), "SmallBlockLandingGear");
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

            Bitmap backgroundBmp = TestLoadTexture(backgroundPath, 0, -1, -1);
            TexUtil.WriteImage(backgroundBmp, @".\TestOutput\BackgroundCube0_Full.png");

            backgroundBmp = TestLoadTexture(backgroundPath, 1, 1024, 1024);
            TexUtil.WriteImage(backgroundBmp, @".\TestOutput\BackgroundCube1_1024.png");

            backgroundBmp = TestLoadTexture(backgroundPath, 2, 512, 512);
            TexUtil.WriteImage(backgroundBmp, @".\TestOutput\BackgroundCube2_512.png");

            backgroundBmp = TestLoadTexture(backgroundPath, 3, 128, 128);
            TexUtil.WriteImage(backgroundBmp, @".\TestOutput\BackgroundCube3_128.png");

            backgroundBmp = TestLoadTexture(backgroundPath, 4, 64, 64);
            TexUtil.WriteImage(backgroundBmp, @".\TestOutput\BackgroundCube4_64.png");

            backgroundBmp = TestLoadTexture(backgroundPath, 5, 32, 32);
            TexUtil.WriteImage(backgroundBmp, @".\TestOutput\BackgroundCube5_32.png");
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

                if (ignoreAlpha)
                {
                    graphics.FillRectangle(Brushes.Black, size * 2, size * 1, size, size);
                    graphics.FillRectangle(Brushes.Black, size * 0, size * 1, size, size);
                    graphics.FillRectangle(Brushes.Black, size * 1, size * 0, size, size);
                    graphics.FillRectangle(Brushes.Black, size * 1, size * 2, size, size);
                    graphics.FillRectangle(Brushes.Black, size * 1, size * 1, size, size);
                    graphics.FillRectangle(Brushes.Black, size * 3, size * 1, size, size);
                }

                graphics.DrawImage(TestLoadTexture(backgroundPath, 0, size, size, ignoreAlpha), size * 2, size * 1, size, size);
                graphics.DrawImage(TestLoadTexture(backgroundPath, 1, size, size, ignoreAlpha), size * 0, size * 1, size, size);
                graphics.DrawImage(TestLoadTexture(backgroundPath, 2, size, size, ignoreAlpha), size * 1, size * 0, size, size);
                graphics.DrawImage(TestLoadTexture(backgroundPath, 3, size, size, ignoreAlpha), size * 1, size * 2, size, size);
                graphics.DrawImage(TestLoadTexture(backgroundPath, 4, size, size, ignoreAlpha), size * 1, size * 1, size, size);
                graphics.DrawImage(TestLoadTexture(backgroundPath, 5, size, size, ignoreAlpha), size * 3, size * 1, size, size);

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
