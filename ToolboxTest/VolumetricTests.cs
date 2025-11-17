using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SEToolbox.Interop;
using SEToolbox.Support;

namespace ToolboxTest
{
    [TestClass]
    public class VolumetricTests
    {
        [TestMethod, TestCategory("UnitTest")]
        public void GenerateModelComplexVolumetric()
        {
            const string modelFile = @".\TestAssets\algos.obj";

            CubeType[][][] cubic = Modelling.ReadVolumetricModel(modelFile, 1, null, ModelTraceVoxel.Thin);

            Dictionary<CubeType, int> cubicCount = Modelling.CountCubic(cubic);

            int size = cubic.Length * cubic[0].Length * cubic[0][0].Length;
            ModelAssertions(cubic, 1290600, 108, 50, 239);

            int x = 54;
            int y = 39;
            int[] zValues = { 7, 17, 18, 19, 20, 23, 24, 25, 26, 35, 36 };
            foreach (int z in zValues)
            {
                Assert.AreEqual(CubeType.Cube, cubic[x][y][z]);
            }

            Assert.AreEqual(51921, cubicCount[CubeType.Cube], "Cube count must match.");
            Assert.AreEqual(188293, cubicCount[CubeType.Interior], "Interior count must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void GenerateModelComplexVolumetricHalfScale()
        {
            const string modelFile = @".\TestAssets\algos.obj";

            CubeType[][][] cubic = Modelling.ReadVolumetricModel(modelFile, 0.5, null, ModelTraceVoxel.Thin);

            Dictionary<CubeType, int> cubicCount = Modelling.CountCubic(cubic);

           
           ModelAssertions(cubic,168480, 54, 26, 120);

            Assert.AreEqual(12540, cubicCount[CubeType.Cube], "Cube count must match.");
            Assert.AreEqual(20651, cubicCount[CubeType.Interior], "Interior count must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void GenerateModelSimpleThinVolumetric()
        {
            const string modelFile = @".\TestAssets\t25.obj";

            CubeType[][][] cubic = Modelling.ReadVolumetricModel(modelFile, 0, null, ModelTraceVoxel.Thin);

            Dictionary<CubeType, int> cubicCount = Modelling.CountCubic(cubic);

            int size = cubic.Length * cubic[0].Length * cubic[0][0].Length;
            ModelAssertions(cubic, 72, 4, 6, 3);

            Assert.AreEqual(36, cubicCount[CubeType.Cube], "Cube count must match.");
            Assert.AreEqual(4, cubicCount[CubeType.Interior], "Interior count must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void GenerateModelSimpleThinSmoothedVolumetric()
        {
            const string modelFile = @".\TestAssets\t25.obj";

            CubeType[][][] cubic = Modelling.ReadVolumetricModel(modelFile, 0, null, ModelTraceVoxel.ThinSmoothed);

            Dictionary<CubeType, int> cubicCount = Modelling.CountCubic(cubic);

            
           ModelAssertions(cubic, 72, 4, 6, 3);
            Assert.AreEqual(36, cubicCount[CubeType.Cube], "Cube count must match.");
            Assert.AreEqual(4, cubicCount[CubeType.Interior], "Interior count must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void GenerateModelSimpleThickVolumetric()
        {
            const string modelFile = @".\TestAssets\t25.obj";

            CubeType[][][] cubic = Modelling.ReadVolumetricModel(modelFile, 0, null, ModelTraceVoxel.Thick);

            Dictionary<CubeType, int> cubicCount = Modelling.CountCubic(cubic);

            
            ModelAssertions(cubic, 72, 4, 6, 3);

            Assert.AreEqual(58, cubicCount[CubeType.Cube], "Cube count must match.");
            Assert.AreEqual(2, cubicCount[CubeType.Interior], "Interior count must match.");
        }

        [TestMethod, TestCategory("UnitTest")]

        public void LoadBrokenModel()
        {
            // Testing the model.
            // TODO: finish testing the model.
            const string modelFile = @".\TestAssets\LibertyStatue.obj";

            CubeType[][][] cubic = Modelling.ReadVolumetricModel(modelFile, 0, null, ModelTraceVoxel.Thin);

            int size = cubic.Length * cubic[0].Length * cubic[0][0].Length;
            ModelAssertions(cubic, 72, 4, 6, 3);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void GenerateModelSimpleVolumetricFill()
        {
            const string modelFile = @".\TestAssets\t25.obj";

            var cubic = Modelling.ReadVolumetricModel(modelFile, 2, null, ModelTraceVoxel.Thin);

            var cubicCount = Modelling.CountCubic(cubic);


            ModelAssertions(cubic,480, 8, 12, 5);

            Assert.AreEqual(168, cubicCount[CubeType.Cube], "Cube count must match.");
            Assert.AreEqual(48, cubicCount[CubeType.Interior], "Interior count must match.");
        }

        [TestMethod, TestCategory("UnitTest")]
        public void GenerateModelSimpleVolumetricAltFill()
        {
            const string modelFile = @".\TestAssets\t25.obj";

            var cubic = Modelling.ReadVolumetricModelAlt(modelFile);

             ModelAssertions(cubic, 480, 8, 12, 5);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void GenerateModelWithMaterial()
        {
            const string modelFile = @".\TestAssets\test.obj";
            CubeType[][][] cubic = Modelling.ReadVolumetricModel(modelFile, 1, null, ModelTraceVoxel.Thin);
            ModelAssertions(cubic, 480, 8, 12, 5);
        }
        public void ModelAssertions(CubeType[][][] cubic, params int[] sizes)
        {
           
            int size = cubic.Length * cubic[0].Length * cubic[0][0].Length;
            var input = sizes ?? cubic.Select(x => x.Length).ToArray();
            foreach (var item in sizes)
            {   
                Assert.AreEqual(item, input[0], "Array  size must match.");
            }
            
        }
        private static long _counter;
        private static long _maximumProgress;
        private static int _percent;
        public void ResetProgress(long initial, long maximumProgress)
        {
            _percent = 0;
            _counter = initial;
            _maximumProgress = maximumProgress;
        }

        public void IncrementProgress()
        {
            _counter++;

            int p = (int)((double)_counter / _maximumProgress * 100);
            if (_percent < p)
            {
                _percent = p;
                SConsole.WriteLine($"{_percent}%");
            }
        }

        [TestMethod, TestCategory("UnitTest")]
        public void IntersectionTestPoint0()
        {
           
            Point3D p1 = new(0, 0, 0),
                    p2 = new(10.999, 0, 0),
                    p3 = new(0, 10.999, -15.999);

            Point3D roundPointA = new(0, 0, -10), 
                    roundPointB = new(0, 0, +10);
            RayChecker(p1, p2, p3, roundPointA, roundPointB, out Point3D intersection, out int normal, false, new Point3D(0, 0, 0),1);
          
        }

        [TestMethod, TestCategory("UnitTest")]
        public void IntersectionTestPoint1()
        {
            Point3D p1 = new(1, 1, 0),
                    p2 = new(10, 1, 0),
                    p3 = new(1, 10, 0);

        List<Point3D> rays =
                [
                    new(1, 1, -10), new(1, 1, +10)
                ];
            RayChecker(p1, p2, p3, rays, out Point3D intersection, out int normal, false, new Point3D(1, 1, 0),1);
          
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestFace()
        {

            Point3D p1 = new(10, 10, 10),
                    p2 = new(15, 15, 11),
                    p3 = new(20, 10, 12);
         
            Point3D roundPointA = new(15, 12, 0),
                    roundPointB = new(15, 12, 20);
            RayChecker(p1, p2, p3, roundPointA, roundPointB, out Point3D intersection, out int normal, false, new Point3D(15, 12, 11));
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestFaceReverse()
        {

            Point3D p1 = new(20, 10, 12),
                    p2 = new(15, 15, 11),
                    p3 = new(10, 10, 10);
            
            Point3D roundPointA = new(15, 12, 0),
                    roundPointB = new(15, 12, 20);
            RayChecker(p1, p2, p3, roundPointA, roundPointB , out Point3D intersection, out int normal, false, new Point3D(15, 12, 11));
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestEdge()
        {

            Point3D p1 = new(10, 10, 10),
                    p2 = new(15, 15, 11),
                    p3 = new(20, 10, 12);
            Point3D roundPointA = new(14, 14, 0), 
                    roundPointB = new(14, 14, 20);
            RayChecker(p1, p2, p3, roundPointA, roundPointB , out Point3D intersection, out int normal, false, new Point3D(14, 14, 10.8));
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestEdgeReverse()
        {

            Point3D p1 = new(20, 10, 12),
                    p2 = new(15, 15, 11),
                    p3 = new(10, 10, 10);
            
            Point3D roundPointA = new(14, 14, 0),
                    roundPointB = new(14, 14, 20);
            RayChecker(p1, p2, p3, roundPointA, roundPointB , out Point3D intersection, out int normal, false, new Point3D(14, 14, 10.8));
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestVertex1()
        {

            Point3D p1 = new Point3D(10, 10, 10), 
                    p2 = new Point3D(15, 15, 11), 
                    p3 = new Point3D(20, 10, 12);
            Point3D roundPointA = new(p1.X, p1.Y, 0), 
                    roundPointB = new(p1.X, p1.Y, 20);

            RayChecker(p1, p2, p3, roundPointA, roundPointB, out Point3D intersection, out int normal, false, p1);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestVertex2()
        {

            Point3D p1 = new(10, 10, 10),
                    p2 = new(15, 15, 11),
                    p3 = new(20, 10, 12);
                    Point3D roundPointA = new(p2.X, p2.Y, 0), 
                            roundPointB = new(p2.X, p2.Y, 20);
            RayChecker(p1, p2, p3, roundPointA, roundPointB, out Point3D intersection, out int normal, false,p2);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestVertex3()
        {

            Point3D p1 = new(10, 10, 10),
                    p2 = new(15, 15, 11),
                    p3 = new(20, 10, 12);
            Point3D roundPointA = new(p3.X, p3.Y, 0),
                    roundPointB = new(p3.X, p3.Y, 20);
            RayChecker(p1, p2, p3, roundPointA, roundPointB, out Point3D intersection, out int normal, false,p3);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestVertexReverse1()
        {

            Point3D p1 = new(20, 10, 12), 
                    p2 = new(15, 15, 11), 
                    p3 = new(10, 10, 10);
            Point3D roundPointA = new(p1.X, p1.Y, 0), 
                    roundPointB = new(p1.X, p1.Y, 20);
            RayChecker(p1, p2, p3, roundPointA, roundPointB, out Point3D intersection, out int normal, false,p1);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestVertexReverse2()
        {

            Point3D p1 = new(20, 10, 12), 
                    p2 = new(15, 15, 11), 
                    p3 = new(10, 10, 10);
            Point3D roundPointA = new(p2.X, p2.Y, 0), 
                    roundPointB = new(p2.X, p2.Y, 20);
            RayChecker(p1, p2, p3, roundPointA, roundPointB, out Point3D intersection, out int normal, false,p2);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestVertexReverse3()
        {

            Point3D p1 = new(20, 10, 12), 
                    p2 = new(15, 15, 11), 
                    p3 = new(10, 10, 10);
            Point3D roundPointA = new(p3.X, p3.Y, 0), 
                    roundPointB = new(p3.X, p3.Y, 20);

            RayChecker(p1, p2, p3, roundPointA, roundPointB, out Point3D intersection, out int normal, false,p3);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestNormalCheck()
        {  
           Point3D  p1 = new Point3D(10, 10, 10),
                    p2 = new Point3D(15, 15, 11),
                    p3 = new Point3D(20, 10, 12);
            Point3D roundPointA = new(15, 12, 2000), 
                    roundPointB = new(15, 12, -2000);
            //bool ret;

            RayChecker(p1, p2, p3, roundPointA, roundPointB,  out Point3D intersection, out int normal,false,1);
            RayChecker(new(10, 10, -990), new(15, 15, 11), new(20, 10, 1012), roundPointA, roundPointB, out intersection, out normal, false,1);
            RayChecker(new(10, 10, 1010), new(15, 15, 11), new(20, 10, -990), roundPointA, roundPointB, out intersection, out normal, false,1);
           
            // reverse ray
            roundPointA = new Point3D(15, 12, -2000);
            roundPointB = new Point3D(15, 12, 2000);

            // reverse normal
                        //p3, p2, p1
            RayChecker(new(10, 10, 10), new(15, 15, 11), new(20, 10, 12), roundPointA, roundPointB, out intersection, out normal, true,1);
            RayChecker(new(10, 10, -990), new(15, 15, 11) , new(20, 10, 1012), roundPointA, roundPointB,out intersection, out normal, true,1);
            RayChecker(new(10, 10, 1010), new(15, 15, 11), new(20, 10, -990), roundPointA, roundPointB, out intersection, out normal, true,1);
         
        }

        [TestMethod, TestCategory("UnitTest")]
        public void RayTestNormalInverseCheck()
        {           
      
             Point3D roundPointA = new(15, 12, 2000), 
                     roundPointB = new(15, 12, -2000);
            //bool ret;
              //point 1, point 2, point 3
            RayChecker(new Point3D(10, 10, 10), new Point3D(15, 15, 11), new Point3D(20, 10, 12), roundPointA, roundPointB, out Point3D intersection, out int normal,  false,-1);    
            RayChecker(new(10, 10, -990), new(15, 15, 11), new(20, 10, 1012), roundPointA, roundPointB, out intersection, out normal, false,-1);
            RayChecker(new(10, 10, 1010), new(15, 15, 11), new(20, 10, -990), roundPointA, roundPointB, out intersection, out normal, false,-1);

            // reverse ray
            roundPointA = new Point3D(15, 12, 2000);
            roundPointB = new Point3D(15, 12, -2000);
            
                // reverse normal
                        // Point1, Point2, Point3, RoundPointA, RoundPointB
            RayChecker(new(10, 10, 10), new(15, 15, 11), new(20, 10, 12), roundPointA, roundPointB,out intersection, out normal, true,-1);
            RayChecker(new(10, 10, -990), new( 15, 15, 11), new(20, 10, 1012), roundPointA, roundPointB,out intersection, out normal,  true,-1);
            RayChecker(new (10, 10, 10), new(15, 15, 11), new(20, 10, -990), roundPointA, roundPointB, out intersection, out normal, true,-1);
          
        }

        public void RayChecker(Point3D p1, Point3D p2, Point3D p3, Point3D roundPointA, Point3D roundPointB, out Point3D intersection, out int normal, bool reverse = false, params object[] parameters)
        {    Point3DCollection points;
             points = [p1, p2, p3];
            
            if (reverse)
            {
                points = [p3, p2, p1];
            }
            bool ret = MeshHelper.RayIntersectTriangleRound(points, roundPointA, roundPointB, out intersection, out normal);
            Assert.IsTrue(ret, "ret must be true.");
            Assert.AreEqual( normal, parameters[0],$"normal must match input {parameters[0]}.");

            if(roundPointA.Z == 0 && roundPointB.Z == 20)
            {
            ret = MeshHelper.RayIntersectTriangleRound(points, roundPointA, roundPointB, out  intersection, out normal);
            Assert.IsTrue(ret, "ret must be true.");
            Assert.AreEqual(parameters[0], intersection, "intersection must match input.");

            }
        }
            public void RayChecker(Point3D p1, Point3D p2, Point3D p3, List<Point3D> rays, out Point3D intersection, out int normal, bool reverse = false, params object[] parameters)
        {    Point3DCollection points;
             points = [p1, p2, p3];
            
            if (reverse)
            {
                points = [p3, p2, p1];
            }
            bool ret = MeshHelper.RayIntersectTriangleRound(points, rays, out intersection, out normal);
            Assert.IsTrue(ret, "ret must be true.");
            Assert.AreEqual(parameters[0], normal ,$"normal must match input {parameters[0]}.");

          

            }         
        }
    }

