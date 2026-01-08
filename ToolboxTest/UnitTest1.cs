using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEToolbox.Interop;
using SEToolbox.Support;
using VRage;
using VRage.Compression;
using VRage.Game;
using VRage.ObjectBuilders.Private;
using VRageMath;
using Color = System.Drawing.Color;
using MOBSerializerKeen = VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen;

namespace ToolboxTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestInitialize]
        public void InitTest()
        {
            try
            {
                SpaceEngineersCore.LoadDefinitions();
            }
            // For debugging tests.
            catch (Exception)
            {
                throw new Exception("SpaceEngineersCore.LoadDefinitions() failed.");
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void GenerateTempFiles()
        {
            for (int i = 0; i < 10; i++)
            {
                string file1 = TempFileUtil.NewFileName(null);
                File.WriteAllBytes(file1, [0x00, 0x01, 0x02]);

                string file2 = TempFileUtil.NewFileName(".txt");
                File.WriteAllText(file2, "blah blah");
            }

            TempFileUtil.Dispose();
        }

        [TestMethod, TestCategory("UnitTest")]
        public void TestImageOptimizer1()
        {
            string fileName = Path.GetFullPath(@".\TestAssets\7242630_orig.jpg");
            System.Drawing.Bitmap bmp = ToolboxExtensions.OptimizeImagePalette(fileName);
            string outputFileTest = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "_optimized" + ".png");
            bmp.Save(outputFileTest, ImageFormat.Png);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void TestImageOptimizer2()
        {
            string fileName = Path.GetFullPath(@".\TestAssets\7242630_scale432.png");
            System.Drawing.Bitmap bmp = ToolboxExtensions.OptimizeImagePalette(fileName);
            string outputFileTest = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "_optimized" + ".png");
            bmp.Save(outputFileTest, ImageFormat.Png);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void TestXmlCompacter1()
        {
            string fileNameSource = Path.GetFullPath(@".\TestAssets\test.xml");
            string fileNameDestination = Path.GetFullPath(@".\TestOutput\test_out.xml");
            ToolboxExtensions.CompactXmlFile(fileNameSource, fileNameDestination);

            long oldFileSize = new FileInfo(fileNameSource).Length;
            long newFileSize = new FileInfo(fileNameDestination).Length;

            Assert.IsTrue(newFileSize < oldFileSize, "new file size must be smaller");
            Assert.AreEqual(2225, oldFileSize, "original file size");
            Assert.AreEqual(1510, newFileSize, "new file size");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void LocateSpaceEngineersApplication()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "SpaceEgineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void ExtractSandboxFromZip()
        {
            const string fileName = @".\TestAssets\SampleWorld.sbw";

            MyObjectBuilder_Checkpoint checkpoint;
            bool result;

            using MyZipArchive archive = MyZipArchive.OpenOnFile(fileName);
            MyZipFileInfo fileInfo = archive.GetFile(SpaceEngineersConsts.SandBoxCheckpointFileName);
            checkpoint = SpaceEngineersApi.TryReadSpaceEngineersFile<MyObjectBuilder_Checkpoint>(fileInfo.GetStream());


            // Use TryReadSpaceEngineersFile with a temporary file path
            string tempFilePath = Path.GetTempFileName();
            using (Stream fileStream = fileInfo.GetStream())
            using (FileStream tempFileStream = File.Create(tempFilePath))
            {
                fileStream.CopyTo(tempFileStream);
            }

            result = SpaceEngineersApi.TryReadSpaceEngineersFile(tempFilePath, out checkpoint, out bool isCompressed, out string errorInformation);

            // Clean up the temporary file
            File.Delete(tempFilePath);


            Assert.IsTrue(result, "Failed to read the Space Engineers file.");
            Assert.IsFalse(isCompressed, "File should not be compressed.");
            Assert.IsNotNull(checkpoint, "Checkpoint should not be null.");
            Assert.AreEqual("Quad Scissor Doors", checkpoint.SessionName, "Checkpoint SessionName must match!");
        }

        // We're not using the zip-extract to folder currently.
        [Ignore]
        [TestMethod]
        public void ExtractZipFileToFolder()
        {
            const string fileName = @".\TestAssets\SampleWorld.sbw";
            const string folder = @".\TestOutput\SampleWorld";

            Assert.IsTrue(File.Exists(fileName), "Source file must exist");

            ZipTools.MakeClearDirectory(folder);

            // Keen's API doesn't know difference between file and folder.
            MyZipArchive.ExtractToDirectory(fileName, folder);

            Assert.IsTrue(File.Exists(Path.Combine(folder, SpaceEngineersConsts.SandBoxCheckpointFileName)), "Destination file must exist");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void ExtractContentFromCompressedSandbox()
        {
            const string fileName = @".\TestAssets\SANDBOX_0_0_0_.sbs";

            const string xmlFileName = @".\TestOutput\SANDBOX_0_0_0_.xml";

            ZipTools.GZipUncompress(fileName, xmlFileName);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void ExtractContentFromXmlSandbox()
        {
            const string fileName = @".\TestAssets\SANDBOX_0_0_0_.XML.sbs";
            bool ret = SpaceEngineersApi.TryReadSpaceEngineersFile(fileName, out MyObjectBuilder_Sector sectorData, out bool isCompressed, out _);

            Assert.IsTrue(ret, "Sandbox content should have been detected");
            Assert.IsFalse(isCompressed, "file should not be compressed");
            Assert.IsNotNull(sectorData, "sectorData != null");
            Assert.IsTrue(sectorData.SectorObjects.Count > 0, "sectorData should be more than 0");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void ExtractContentFromProtoBufSandbox()
        {
            //fileName will automatically be concatenated with the ProtobuffersExtension.
            const string fileName = @".\TestAssets\SANDBOX_0_0_0_.Proto.sbs";
            bool ret = SpaceEngineersApi.TryReadSpaceEngineersFile(fileName, out MyObjectBuilder_Sector sectorData, out bool isCompressed, out _);

            Assert.IsTrue(ret, "Sandbox content should have been detected");
            Assert.IsFalse(isCompressed, "file should not be compressed");
            Assert.IsNotNull(sectorData, "sectorData != null");
            Assert.IsTrue(sectorData.SectorObjects.Count > 0, "sectorData should be more than 0");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void SandboxColorTest()
        {
            Color[] colors =
            [
                Color.FromArgb(255, 255, 255),
                Color.FromArgb(0, 0, 0),
                Color.FromArgb(0, 1, 1),   // PhotoShop = H:180, S:100%, B:1%
                Color.FromArgb(255, 0, 0),
                Color.FromArgb(0, 255, 0),
                Color.FromArgb(0, 0, 255),
            ];

            List<SerializableVector3> hsvList = [];
            foreach (Color color in colors)
            {
                hsvList.Add(color.FromPaletteColorToHsvMask());
            }

            List<Color> rgbList = [];
            foreach (SerializableVector3 hsv in hsvList)
            {
                rgbList.Add(hsv.FromHsvMaskToPaletteColor());
            }

            Color[] rgbArray = [.. rgbList];

            for (int i = 0; i < colors.Length; i++)
            {
                Assert.AreEqual(rgbArray[i].R, colors[i].R, "Red Should Equal");
                Assert.AreEqual(rgbArray[i].B, colors[i].B, "Blue Should Equal");
                Assert.AreEqual(rgbArray[i].G, colors[i].G, "Green Should Equal");
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VRageColorTest()
        {
            Color c1 = Color.FromArgb(255, 255, 255);
            Color c2 = Color.FromArgb(0, 0, 0);
            Color c3 = Color.FromArgb(0, 1, 1);   //PS = H:180, S:100%, B:1%

            Vector3 vColor1 = ColorExtensions.ColorToHSV(new VRageMath.Color(c1.R, c1.B, c1.G));
            Vector3 vColor2 = ColorExtensions.ColorToHSV(new VRageMath.Color(c2.R, c2.B, c2.G));
            Vector3 vColor3 = ColorExtensions.ColorToHSV(new VRageMath.Color(c3.R, c3.B, c3.G));

            VRageMath.Color rgb1 = ColorExtensions.HSVtoColor(vColor1);
            VRageMath.Color rgb2 = ColorExtensions.HSVtoColor(vColor2);
            VRageMath.Color rgb3 = ColorExtensions.HSVtoColor(vColor3);

            Assert.AreEqual(rgb1.R, c1.R, "Red Should Equal");
            Assert.AreEqual(rgb1.B, c1.B, "Blue Should Equal");
            Assert.AreEqual(rgb1.G, c1.G, "Green Should Equal");

            Assert.AreEqual(rgb2.R, c2.R, "Red Should Equal");
            Assert.AreEqual(rgb2.B, c2.B, "Blue Should Equal");
            Assert.AreEqual(rgb2.G, c2.G, "Green Should Equal");

            Assert.AreEqual(rgb3.R, c3.R, "Red Should Equal");
            Assert.AreEqual(rgb3.B, c3.B, "Blue Should Equal");
            Assert.AreEqual(rgb3.G, c3.G, "Green Should Equal");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void VRageHSVColorTest()
        {
            // hue Slider is 0~360. Stored as 0~1.
            // saturation Slider is 0~1. Stored as -1~1.
            // value Slider is 0~1. Stored as -1~1.

            Dictionary<string, SerializableVector3> hsvColors = new()
            {
#if DEBUG            
                {"test range 1", new SerializableVector3(0, -1, -1)},
                {"test range 2", new SerializableVector3(0.5f, 0, 0)},
                {"test range 3", new SerializableVector3(1, 1, 1)},
#endif
                {"INGAME dark red", new SerializableVector3(0f,0f,0.05f)},
                {"INGAME dark green", new SerializableVector3(0.333333343f,-0.48f,-0.25f)},
                {"INGAME dark blue", new SerializableVector3(0.575f,0f,0f)},
                {"INGAME dark yellow", new SerializableVector3(0.122222222f,-0.1f,0.26f)},
                {"INGAME dark white", new SerializableVector3(0f,-0.8f,0.4f)},
                {"INGAME black", new SerializableVector3(0f,-0.96f,-0.5f)},
                {"INGAME light gray", new SerializableVector3(0f,-0.85f,0.2f)},
                {"INGAME light red", new SerializableVector3(0f,0.15f,0.25f)},
                {"INGAME light green", new SerializableVector3(0.333333343f,-0.33f,-0.05f)},
                {"INGAME light blue", new SerializableVector3(0.575f,0.15f,0.2f)},
                {"INGAME light yellow", new SerializableVector3(0.122222222f,0.05f,0.46f)},
                {"INGAME white", new SerializableVector3(0f,-0.8f,0.6f)},
                {"INGAME dark dark gray", new SerializableVector3(0f,-0.81f,-0.13f)},
            };

            foreach (KeyValuePair<string, SerializableVector3> kvp in hsvColors)
            {
                string name = kvp.Key;
                SerializableVector3 storedHsv = kvp.Value;
                Color myColor1 = storedHsv.FromHsvMaskToPaletteColor();
                SerializableVector3 newHsv = myColor1.FromPaletteColorToHsvMask();
                Color myColor2 = ((SerializableVector3)newHsv).FromHsvMaskToPaletteColor();
                Assert.AreEqual(myColor1, myColor2, $"Color '{name}' Should Equal");
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void SingleConversionTest()
        {
            float f1 = -17.6093254f;
            float f2 = 72.215f;
            float f3 = -218.569977f;

            double d1 = Convert.ToDouble(f1);
            float g1 = Convert.ToSingle(d1);

            double d2 = Convert.ToDouble(f2);
            float g2 = Convert.ToSingle(d2);

            double d3 = Convert.ToDouble(f3);
            float g3 = Convert.ToSingle(d3);

            Assert.AreEqual(f1, g1, "Should Equal");
            Assert.AreEqual(f2, g2, "Should Equal");
            Assert.AreEqual(f3, g3, "Should Equal");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void BoundingBoxIntersectKeen()
        {
            Vector3D point = new(5d, 3.5d, 4d);
            Vector3D vector = new(-0.03598167d, 0.0110336d, 0.9992915d);
            BoundingBoxD box = new(new(3d, 3d, 2d), new(7d, 4d, 6d));
            RayD ray = new(point, vector);

            double? f = box.Intersects(ray);

            Assert.AreEqual(0, f, "Should Equal");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void BoundingBoxIntersectCustom()
        {
            Vector3D point = new(5d, 3.5d, 4d);
            Vector3D vector = new(-0.03598167d, 0.0110336d, 0.9992915d);
            BoundingBoxD box = new(new Vector3D(3d, 3d, 2d), new Vector3D(7d, 4d, 6d));

            Vector3D? p = box.IntersectsRayAt(point, vector * 1000);

            Assert.AreEqual(new Vector3D(4.9176489098920966d, 3.5151384795308953d, 6.00000000000319d), p.Value, "Should Equal");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void CubeRotate()
        {
            MyPositionAndOrientation positionOrientation = new(new Vector3D(10, 10, 10), Vector3.Backward, Vector3.Up);
            MyCubeSize gridSizeEnum = MyCubeSize.Large;

            MyObjectBuilder_CubeBlock cube = (MyObjectBuilder_CubeBlock)MOBSerializerKeen.CreateNewObject(typeof(MyObjectBuilder_CubeBlock), "LargeBlockArmorBlock");
            //var cube = (MyObjectBuilder_CubeBlock)MyObjectBuilderSerializerKeen.CreateNewObject(typeof(MyObjectBuilder_Thrust), "LargeBlockLargeThrust");
            cube.Min = new SerializableVector3I(10, 10, 10);
            cube.BlockOrientation = new SerializableBlockOrientation(Base6Directions.Direction.Forward, Base6Directions.Direction.Up);
            cube.BuildPercent = 1;

            QuaternionD quaternion = positionOrientation.ToQuaternionD();
            Sandbox.Definitions.MyCubeBlockDefinition definition = SpaceEngineersApi.GetCubeDefinition(cube.TypeId, gridSizeEnum, cube.SubtypeName);

            Vector3I orientSize = definition.Size.Transform(cube.BlockOrientation).Abs();
            Vector3D min = cube.Min.ToVector3D() * gridSizeEnum.ToLength();
            Vector3D max = (cube.Min + orientSize).ToVector3D() * gridSizeEnum.ToLength();
            Vector3D p1 = min.Transform(quaternion) + positionOrientation.Position;
            Vector3D p2 = max.Transform(quaternion) + positionOrientation.Position;
            new BoundingBoxD(p1, p2);
        }

        /// <summary>
        /// This test is critical for rotation a station. For some reason,
        /// if the rotation is not exactly 1, or 0, then there is an issue placing cubes on the station.
        /// </summary>
        [TestMethod, TestCategory("UnitTest")]
        public void Rotation()
        {
            MyPositionAndOrientation positionAndOrientation = new(
                                     new(10.0d, -10.0d, -2.5d),
                                     new(0.0f, 0.0f, -1.0f),
                                     new(0.0f, 1.0f, 0.0f));

            // -90 around Z
            Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(0, 0, -MathHelper.PiOver2);
            Quaternion o = positionAndOrientation.ToQuaternion() * quaternion;
            Quaternion on = Quaternion.Normalize(o);
            Matrix m = on.ToMatrix();
            m = Matrix.Round(ref m);
            MyPositionAndOrientation p = new(m);

            QuaternionD quaternion2 = QuaternionD.CreateFromYawPitchRoll(0, 0, -Math.PI / 2);
            QuaternionD o2 = positionAndOrientation.ToQuaternionD() * quaternion2;
            QuaternionD on2 = QuaternionD.Normalize(o2);
            new MyPositionAndOrientation(on2.ToMatrixD());

            System.Windows.Media.Media3D.Quaternion quaternion3 = new(new System.Windows.Media.Media3D.Vector3D(0, 0, 1), -90d);
            QuaternionD x3 = positionAndOrientation.ToQuaternionD();
            System.Windows.Media.Media3D.Quaternion o3 = new System.Windows.Media.Media3D.Quaternion(x3.X, x3.Y, x3.Z, x3.W) * quaternion3;
            System.Windows.Media.Media3D.Quaternion on3 = o3;
            on3.Normalize();

            double num = on3.X * on3.X;
            double num3 = on3.Z * on3.Z;
            double num4 = on3.X * on3.Y;
            double num5 = on3.Z * on3.W;
            double num8 = on3.Y * on3.Z;
            double num9 = on3.X * on3.W;
            double M21 = 2.0d * (num4 - num5);
            double M22 = 1.0d - 2.0d * (num3 + num);
            double M23 = 2.0d * (num8 + num9);
            new Vector3D(M21, M22, M23);
            SerializableVector3 fwd = new(0.0f, 0.0f, -1.0f);
            SerializableVector3 up = new(1.0f, 0.0f, 0.0f);

            Assert.AreEqual(fwd.X, p.Forward.X, "Forward.X Should Equal");
            Assert.AreEqual(fwd.Y, p.Forward.Y, "Forward.Y Should Equal");
            Assert.AreEqual(fwd.Z, p.Forward.Z, "Forward.Z Should Equal");
            Assert.AreEqual(up.X, p.Up.X, "Up.X Should Equal");
            Assert.AreEqual(up.Y, p.Up.Y, "Up.Y Should Equal");
            Assert.AreEqual(up.Z, p.Up.Z, "Up.Z Should Equal");
        }

        /// <summary>
        /// This test to to verify that the ebpage and RegEx pattern for finding the version and url of the current version of SEToolbox still works.
        /// </summary>
        [TestMethod, TestCategory("UnitTest")]
        public void CheckCurrentVersion()
        {
            ApplicationRelease update = CodeRepositoryReleases.CheckForUpdates(new Version(), true);
            Assert.IsNotNull(update);
            Assert.IsNotNull(update.Version);
            Assert.IsNotNull(update.Link);
        }
    }
}
