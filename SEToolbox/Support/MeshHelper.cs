using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

using HelixToolkit.Wpf;
using SEToolbox.Models;

namespace SEToolbox.Support
{
    public static class MeshHelper
    {
        public static Model3DGroup Load(string path, Dispatcher dispatcher = null, bool freeze = false, bool ignoreErrors = false)
        {
            if (string.IsNullOrEmpty(path))
            {
               return null; 
            }

            Material defaultMaterial = Materials.Blue;

            dispatcher ??= Dispatcher.CurrentDispatcher;

            string ext = Path.GetExtension(path).ToLower();
            Model3DGroup model;

            Dictionary<string, Func<Dispatcher, Material, bool, bool, Model3DGroup>> readers = new()
            {

                { ".3ds",  (d, m, f, i) => new StudioReader(d).Read(path) },
                { ".lwo",  (d, m, f, i) => new LwoReader(d).Read(path)  },
                { ".obj",  (d, m, f, i) => new ObjReader(d).Read(path)  },
                { ".objx", (d, m, f, i) => new ObjReader(d).ReadZ(path) },
                { ".off",  (d, m, f, i) => new OffReader(d).Read(path)  },
                { ".stl",  (d, m, f, i) => new StLReader(d).Read(path)  }

            };
            if (readers.TryGetValue(ext, out Func<Dispatcher, Material, bool, bool, Model3DGroup> readerFunc))
            {
                model = readerFunc.Invoke(dispatcher, defaultMaterial, freeze, ignoreErrors);
            }
            else
            {
                throw new InvalidOperationException("File format not supported.");
            }

            return model;
        }

        public static void TransformScale(this Model3DGroup model, double scale)
        {
            TransformScale(model, scale, scale, scale);
        }

        public static void TransformScale(this Model3DGroup model, double scaleX, double scaleY, double scaleZ)
        {
            foreach (MeshGeometry3D g in from GeometryModel3D gm in model.Children
                                         let g = gm.Geometry as MeshGeometry3D
                                         where g != null
                                         select g)
            {
                for (int i = 0; i < g.Positions.Count; i++)
                {
                    g.Positions[i] = new Point3D(g.Positions[i].X * scaleX, g.Positions[i].Y * scaleY, g.Positions[i].Z * scaleZ);
                }

                if (g.Normals != null)
                {
                    for (int i = 0; i < g.Normals.Count; i++)
                    {
                        g.Normals[i] = new Vector3D(g.Normals[i].X * scaleX, g.Normals[i].Y * scaleY, g.Normals[i].Z * scaleZ);
                        g.Normals[i].Normalize();
                    }
                }
            }
        }

        public static Point3DCollection Point3DsToPoints3D(this IEnumerable<Point3D> point3Ds)
        {
            return [.. point3Ds.Select(p => new Point3D(p.X, p.Y, p.Z))];
        }

        public static bool RayIntersectTriangle(Point3DCollection rayPoints, Point3D roundPointA, Point3D roundPointB,out Point3D intersection, out int norm)
        {
            // http://gamedev.stackexchange.com/questions/5585/line-triangle-intersection-last-bits

            intersection = default;
            norm = 0;

            // Find Triangle Normal
            Vector3D normal = Vector3D.CrossProduct(rayPoints[1] - rayPoints[0], rayPoints[2] - rayPoints[0]);
            normal.Normalize();

            // not a triangle. Two or more points may occupy the same place.
            if (normal.IsUndefined())
            {
                return false;
            }

            // Find distance from LP1 and LP2 to the plane defined by the triangle
            double dist1 = Vector3D.DotProduct(roundPointA - rayPoints[0], normal);
            double dist2 = Vector3D.DotProduct(roundPointB - rayPoints[0], normal);

            if ((dist1 * dist2) >= 0.0f)
            {
                // line doesn't cross the triangle.
                return false;
            }

            if (dist1 == dist2)
            {
                // line and plane are parallel
                return false;
            }
    
            // Find point on the line that intersects with the plane.
            bool complexCalc = false;
            Point3D intersectPos = default;
            switch (complexCalc)
            {
                case false:
                    intersectPos = roundPointA + (roundPointB - roundPointA) * (-dist1 / (dist2 - dist1));
                    break;
                case true:// Alternate calculation, but slower.
                    intersectPos = roundPointA + Vector3D.DotProduct(normal,rayPoints[0] - roundPointA) / Vector3D.DotProduct(normal,roundPointB - roundPointA) * (roundPointB - roundPointA);
                    break;

            }

            // Find if the interesection point lies inside the triangle by testing it against all edges
          
              var vTest = Vector3D.CrossProduct(normal, rayPoints[1] - rayPoints[0]);
            var dTest = Vector3D.DotProduct(vTest, intersectPos - rayPoints[0]);

            if (dTest < 0.0f)
            {
                return false;
            }

            if (Vector3D.DotProduct(vTest, intersectPos - rayPoints[1]) < 0.0f ||
                Vector3D.DotProduct(vTest, intersectPos - rayPoints[2]) < 0.0f)
            {
                return false;
            }

            vTest = Vector3D.CrossProduct(normal, rayPoints[0] - rayPoints[1]);
            dTest = Vector3D.DotProduct(vTest, intersectPos - rayPoints[2]);
            if (dTest < 0.0f)
            {
                // no intersect P2-P1
                return false;
            }

            vTest = Vector3D.CrossProduct(normal, rayPoints[2] - rayPoints[1]);
            dTest = Vector3D.DotProduct(vTest, intersectPos - rayPoints[1]);
            if (dTest < 0.0f)
            {
                // no intersect P3-P2
                return false;
            }

            vTest = Vector3D.CrossProduct(normal, rayPoints[0] - rayPoints[2]);
            dTest = Vector3D.DotProduct(vTest, intersectPos - rayPoints[2]);
            if (dTest < 0.0f)
            {
                // no intersect P1-P3
                return false;
            }

            // Determine if Normal is facing towards or away from Ray.
            norm = Math.Sign(Vector3D.DotProduct(roundPointB - roundPointA, normal));

            intersection = intersectPos;

            return true;
        }


        public static bool RayIntersectTriangleRound(Point3DCollection rayPoints, List<Point3D> rays, out Point3D intersection, out int normal)
        {
            for (int i = 0; i < rays.Count; i += 2)
            {
                if (RayIntersectTriangleRound(rayPoints, rays[i], rays[i + 1], out intersection, out normal)|| // Ray
                    RayIntersectTriangleRound(rayPoints, rays[i + 1], rays[i], out intersection, out normal)) // Reverse Ray
                    return true;
            }

            intersection = default;
            norm = 0;
            return false;
        }

        public static bool RayIntersectTriangleRound(Point3DCollection rayPoints, Point3D roundPointA, Point3D roundPointB, out Point3D intersection, out int norm)
        {
            // http://gamedev.stackexchange.com/questions/5585/line-triangle-intersection-last-bits

            intersection = default;
            norm = 0;

            const int rounding = 14;

            // Find Triangle Normal
            Vector3D normal = CrossProductRound(Round(rayPoints[2] - rayPoints[0], rounding), Round(rayPoints[2] - rayPoints[0], rounding));
            normal.Normalize();

            // not a triangle. Two or more points may occupy the same place.
            if (normal.IsUndefined())
            {
                return false;
            }

            // Find distance from LP1 and LP2 to the plane defined by the triangle
            double dist1 = DotProductRound(Round(roundPointA - rayPoints[0], rounding), normal);
            double dist2 = DotProductRound(Round(roundPointB - rayPoints[0], rounding), normal);

            if ((dist1 * dist2) >= 0.0f)
            {
                // line doesn't cross the triangle.
                return false;
            }

            if (dist1 == dist2)
            {
                // ray line and plane are parallel
                return false;
            }

            // Find point on the line that intersects with the plane
            // Rouding to correct for anonymous rounding issues! Damn doubles!
            Point3D intersectPos =  roundPointA + (roundPointB - roundPointA) * Math.Round(-dist1 / Math.Round(dist2 - dist1, rounding), rounding);

            // Find if the interesection point lies inside the triangle by testing it against all edges

            Vector3D vTest = Vector3D.CrossProduct(normal, rayPoints[1] - rayPoints[0]);
            //var vTest = CrossProductRound(normal, Round(p2 - p1, rounding));
            //var vTest = CrossProductRound(normal, Round(rayPoints[1] - rayPoints[0], rounding));
            if (DotProductRound(vTest, Round(intersectPos - rayPoints[0], rounding)) < 0.0f)
                if (Math.Round(Vector3D.DotProduct(vTest, intersectPos - rayPoints[0]), 12) < 0.0f)
                {
                    // No intersection on edge P2-P1.
                    return false;
                }

            vTest = Vector3D.CrossProduct(normal, rayPoints[0] - rayPoints[1]);
            //vTest = CrossProductRound(normal, Round(rayPoints,[0] - rayPoints[1], rounding));
            //if (DotProductRound(vTest, Round(intersectPos - rayPoints[1], rounding)) < 0.0f)
            if (Math.Round(Vector3D.DotProduct(vTest, intersectPos - rayPoints[1]), 12) < 0.0f)
            {
                // No intersection on edge P3-P2.
                return false;
            }

            vTest = Vector3D.CrossProduct(normal, rayPoints[0] - rayPoints[0]);
            //vTest =  CrossProductRound(normal, Round(rayPoints,[0] - rayPoints[0], rounding));
            //if (DotProductRound(vTest, Round(intersectPos - rayPoints[0], rounding)) < 0.0f)
            if (Math.Round(Vector3D.DotProduct(vTest, intersectPos - rayPoints[0]), 12) < 0.0f)
            {
                // No intersection on edge P1-P3.
                return false;
            }

            // Determine if Normal is facing towards or away from Ray.
            norm = Math.Sign(DotProductRound(roundPointB - roundPointA, normal));

            intersection = intersectPos;

            return true;
        }

        public static Point3D Floor(this Point3D point)
        {
            return new(Math.Floor(point.X),
                       Math.Floor(point.Y),
                       Math.Floor(point.Z));
        }

        public static Point3D Ceiling(this Point3D point)
        {
            return new(Math.Ceiling(point.X),
                       Math.Ceiling(point.Y),
                       Math.Ceiling(point.Z));
        }

        internal static Vector3D Round(this Vector3D vector)
        {
            return new(Math.Round(vector.X),
                       Math.Round(vector.Y),
                       Math.Round(vector.Z));
        }

        internal static Vector3D Round(this Vector3D vector, int places)
        {
            return new(Math.Round(vector.X, places),
                       Math.Round(vector.Y, places),
                       Math.Round(vector.Z, places));
        }

        internal static Point3D Round(this Point3D point)
        {
            return new(Math.Round(point.X),
                       Math.Round(point.Y),
                       Math.Round(point.Z));
        }

        internal static Point3D Round(this Point3D point, int places)
        {
            return new(Math.Round(point.X, places),
                       Math.Round(point.Y, places),
                       Math.Round(point.Z, places));
        }

        internal static Vector3D CrossProductRound(Vector3D vector1, Vector3D vector2)
        {
            return new Vector3D
            {
                X = Math.Round(Math.Round(vector1.Y * vector2.Z, 14) - Math.Round(vector1.Z * vector2.Y, 14), 14),
                Y = Math.Round(Math.Round(vector1.Z * vector2.X, 14) - Math.Round(vector1.X * vector2.Z, 14), 14),
                Z = Math.Round(Math.Round(vector1.X * vector2.Y, 14) - Math.Round(vector1.Y * vector2.X, 14), 14)
            };
        }

        internal static double DotProductRound(Vector3D vector1, Vector3D vector2)
        {
            return Math.Round(Math.Round(Math.Round(vector1.X * vector2.X, 14) + Math.Round(vector1.Y * vector2.Y, 14), 14) + Math.Round(vector1.Z * vector2.Z, 14), 13);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="roll"></param>
        /// <param name="yaw"></param>
        /// <param name="pitch"></param>
        /// <returns></returns>
        public static Transform3D TransformVector(Vector3D origin, double roll, double yaw, double pitch)
        {
            Transform3DGroup transform = new();
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new(0, 0, 1), yaw))); // y angle
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new(0, -1, 0), roll))); // z angle
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new(-1, 0, 0), pitch))); // x angle
            transform.Children.Add(new TranslateTransform3D(origin));
            return transform;
        }

        public static Point3D Min(Point3D point1, Point3D point2, Point3D point3)
        {
            return new Point3D(Math.Min(Math.Min(point1.X, point2.X), point3.X),
                               Math.Min(Math.Min(point1.Y, point2.Y), point3.Y),
                               Math.Min(Math.Min(point1.Z, point2.Z), point3.Z));
        }

        public static Point3D Max(Point3D point1, Point3D point2, Point3D point3)
        {
            return new Point3D(Math.Max(Math.Max(point1.X, point2.X), point3.X),
                               Math.Max(Math.Max(point1.Y, point2.Y), point3.Y),
                               Math.Max(Math.Max(point1.Z, point2.Z), point3.Z));
        }
    }
}
