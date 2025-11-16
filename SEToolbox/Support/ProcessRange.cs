using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace SEToolbox.Support
{
    public static class PRange
    {
        public static IEnumerable<Vector3I> ProcessRange(Vector3I size)
        {
            var vRange = from x in Enumerable.Range(0, size.X)
                         from y in Enumerable.Range(0, size.Y)
                         from z in Enumerable.Range(0, size.Z)
                         select new Vector3I(x, y, z);


            Vector3I v = Vector3I.Zero;
            foreach (Vector3I vec in vRange)
            {
                v.X = vec.X;
                v.Y = vec.Y;
                v.Z = vec.Z;
                yield return v;
            }
        }

        public static IEnumerable<Vector3I> ProcessRange(Vector3I v, Vector3I size)
        {
            var vRange = Enumerable.Range(0, size.X * size.Y * size.Z).Select(i => new Vector3I(i % size.X, (i / size.X) % size.Y, i / (size.X * size.Y)));

            foreach (Vector3I vec in vRange)
            {
                v.X = vec.X;
                v.Y = vec.Y;
                v.Z = vec.Z;
                yield return v;
            }
        }

        public static Vector3I ProcessRange(int x, int y, int z, Vector3I size)
        {
            var vRange = from X in Enumerable.Range(0, size.X)
                         from Y in Enumerable.Range(0, size.Y)
                         from Z in Enumerable.Range(0, size.Z)
                         select new Vector3I(x, y, z);

            Vector3I v = Vector3I.Zero;
            foreach (Vector3I vec in vRange)
            {
                v.X = vec.X;
                v.Y = vec.Y;
                v.Z = vec.Z;
            }
            return v;
        }
        public static Vector3I ProcessRange(Vector3I v, int rangeMin, int rangeMax)
        {

            var vRange = from X in Enumerable.Range(rangeMin, rangeMax)
                         from Y in Enumerable.Range(rangeMin, rangeMax)
                         from Z in Enumerable.Range(rangeMin, rangeMax)
                         select new Vector3I(X, Y, Z);


            foreach (Vector3I vec in vRange)
            {
                v.X = vec.X;
                v.Y = vec.Y;
                v.Z = vec.Z;
            }
            return v;
        }

        public static Vector3I ProcessRange(Vector3I v, int x = 0, int y = 0, int z = 0, int rangeMin = 0, int rangeMax = 0)
        {
            var vRange = from X in Enumerable.Range(rangeMin, rangeMax - rangeMin + 1)
                         from Y in Enumerable.Range(rangeMin, rangeMax - rangeMin + 1)
                         from Z in Enumerable.Range(rangeMin, rangeMax - rangeMin + 1)
                         select new Vector3I(x + X, y + Y, z + Z);

            Vector3I min = vRange.Min();
            Vector3I max = vRange.Max();

            return new Vector3I(
                v.X + (max.X - min.X),
                v.Y + (max.Y - min.Y),
                v.Z + (max.Z - min.Z));
        }

        public static Vector3I ProcessRange(Vector3I v, int x, int y, int z, Vector3I size)
        {
            var vRange = Enumerable.Range(0, size.X * size.Y * size.Z).Select(i => new Vector3I(v.X + (i % size.X) + x, v.Y + ((i / size.X) % size.Y) + y, v.Z + (i / (size.X * size.Y)) + z));


            foreach (Vector3I vec in vRange)
            {
                v.X = vec.X;
                v.Y = vec.Y;
                v.Z = vec.Z;
            }
            return v;
        }

 
    }




}