using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox.Definitions;
using SEToolbox.Interop;
using SEToolbox.Interop.Models;
using SEToolbox.Support;
using MOBTypes = SEToolbox.Interop.SpaceEngineersTypes.MOBTypeIds;

namespace ToolboxTest
{
    [TestClass]
    public class ModelTests
    {
        [TestInitialize]
        public void InitTest()
        {
            SpaceEngineersCore.LoadDefinitions();
        }

        // This is ignored because this hasn't been implemented in the Toolbox as yet.
        [Ignore]
        [TestMethod]
        public void BaseModel1LoadSave()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            MyCubeBlockDefinition largeThruster = (MyCubeBlockDefinition)MyDefinitionManager.Static.GetDefinition(MOBTypes.Thrust, "LargeBlockLargeThrust");
            string thrusterModelPath = Path.Combine(contentPath, largeThruster.Model);
            Assert.IsTrue(File.Exists(thrusterModelPath), "Filepath should exist on developer machine");

            Dictionary<string, object> modelData = MyModel.LoadModelData(thrusterModelPath);
            //var modelData = MyModel.LoadCustomModelData(thrusterModelPath);

            string testFilePath = @".\TestOutput\Thruster.mwm";
            MyModel.SaveModelData(testFilePath, modelData);

            byte[] originalBytes = File.ReadAllBytes(thrusterModelPath);
            byte[] newBytes = File.ReadAllBytes(testFilePath);

            Assert.AreEqual(originalBytes.Length, newBytes.Length, "Bytestream content must equal");
            Assert.IsTrue(originalBytes.SequenceEqual(newBytes), "Bytestream content must equal");
        }

        // This is ignored because this hasn't been implemented in the Toolbox as yet.
        [Ignore]
        [TestMethod, TestCategory("UnitTest")]
        public void CustomModel1LoadSave()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            string cockpitModelPath = Path.Combine(contentPath, @"Models\Characters\Animations\cockpit1_large.mwm");
            Assert.IsTrue(File.Exists(cockpitModelPath), "Filepath should exist on developer machine");

            Dictionary<string, object> modelData = MyModel.LoadCustomModelData(cockpitModelPath);

            string testFilePath = @".\TestOutput\cockpit_animation.mwm";
            MyModel.SaveModelData(testFilePath, modelData);

            byte[] originalBytes = File.ReadAllBytes(cockpitModelPath);
            byte[] newBytes = File.ReadAllBytes(testFilePath);

            Assert.AreEqual(originalBytes.Length, newBytes.Length, "Bytestream content must equal");
            Assert.IsTrue(originalBytes.SequenceEqual(newBytes), "Bytestream content must equal");
        }

        [Ignore]
        [TestMethod]
        public void LoadModelFailures()
        {
            string location = ToolboxUpdater.GetApplicationFilePath();
            Assert.IsNotNull(location, "Space Engineers should be installed on developer machine");
            Assert.IsTrue(Directory.Exists(location), "Filepath should exist on developer machine");

            string contentPath = ToolboxUpdater.GetApplicationContentPath();

            string[] files = Directory.GetFiles(Path.Combine(contentPath, "Models"), "*.mwm", SearchOption.AllDirectories);
            List<string> badList = [];
            List<string> convertDiffers = [];

            foreach (string file in files)
            {
                Dictionary<string, object> data;
                try
                {
                    data = MyModel.LoadModelData(file);
                    data = MyModel.LoadCustomModelData(file);
                }
                catch (Exception)
                {
                    badList.Add(file);
                    continue;
                }

                if (data != null)
                {
                    string testFilePath = @".\TestOutput\TempModelTest.mwm";

                    MyModel.SaveModelData(testFilePath, data);

                    byte[] originalBytes = File.ReadAllBytes(file);
                    byte[] newBytes = File.ReadAllBytes(testFilePath);

                    if (!originalBytes.SequenceEqual(newBytes))
                    {
                        convertDiffers.Add(file);
                    }

                    //Assert.AreEqual(originalBytes.Length, newBytes.Length, $"File {file} Bytestream content must equal");
                    //Assert.IsTrue(originalBytes.SequenceEqual(newBytes), $"File {file} Bytestream content must equal");
                }
            }

            Assert.IsTrue(convertDiffers.Count > 0, "");
            Assert.IsTrue(badList.Count > 0, "");
        }
    }
}
