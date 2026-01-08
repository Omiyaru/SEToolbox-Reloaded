using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using VRageMath;

namespace SEToolbox.Support
{
    public static class PRange
    {

        public static IEnumerable<Vector3I> ProcessRange(Vector3I v, Vector3I size)
        {
            IEnumerable<Vector3I> vRange = from x in Enumerable.Range(0, size.X)
                         from y in Enumerable.Range(0, size.Y)
                         from z in Enumerable.Range(0, size.Z)
                         select new Vector3I(x, y, z);

            Parallel.ForEach(vRange, vec =>
             {
                 v.X = vec.X;
                 v.Y = vec.Y;
                 v.Z = vec.Z;
             });
            return vRange;
        }

        public static Vector3I ProcessRange(int x, int y, int z, Vector3I size)
        {
            IEnumerable<Vector3I> vRange = from X in Enumerable.Range(0, size.X)
                                           from Y in Enumerable.Range(0, size.Y)
                                           from Z in Enumerable.Range(0, size.Z)
                                           select new Vector3I(x, y, z);

            Vector3I v = Vector3I.Zero;
            Parallel.ForEach(vRange, vec =>
            {
                v.X = vec.X;
                v.Y = vec.Y;
                v.Z = vec.Z;
            });
            return v;
        }

        public static Vector3I ProcessRange(Vector3I v, int x = 0, int y = 0, int z = 0, Vector3I size = default)
        {
            if (v == default)
            {
                v = Vector3I.Zero;
            }

            IEnumerable<Vector3I> vRange = from X in Enumerable.Range(0, size.X)
                         from Y in Enumerable.Range(0, size.Y)
                         from Z in Enumerable.Range(0, size.Z)
                         select new Vector3I(x, y, z);

            Parallel.ForEach(vRange, vec =>
            {
                v.X = vec.X;
                v.Y = vec.Y;
                v.Z = vec.Z;
            });
            return v;
        }

        public static Vector3I ProcessRange(Vector3I v, int rangeMin, int rangeMax)
        {

            IEnumerable<Vector3I> vRange = from X in Enumerable.Range(rangeMin, rangeMax)
                         from Y in Enumerable.Range(rangeMin, rangeMax)
                         from Z in Enumerable.Range(rangeMin, rangeMax)
                         select new Vector3I(X, Y, Z);


            Parallel.ForEach(vRange, vec =>
             {
                 v.X = vec.X;
                 v.Y = vec.Y;
                 v.Z = vec.Z;
             });
            return v;
        }


        public static Vector3I ProcessRange(Vector3I v, int x = 0, int y = 0, int z = 0, int rangeMin = 0, int rangeMax = 0)
        {

            IEnumerable<Vector3I> vRange = from X in Enumerable.Range(rangeMin, rangeMax)
                         from Y in Enumerable.Range(rangeMin, rangeMax)
                         from Z in Enumerable.Range(rangeMin, rangeMax)
                         select new Vector3I(x, y, z);

            Parallel.ForEach(vRange, vec =>
            {
                v.X = vec.X;
                v.Y = vec.Y;
                v.Z = vec.Z;
            });
            return v;
        }
     
        public static IEnumerable<int> ProcessRange(int[] v, int[] size)
        {

            v = v == default ? [] : v;

            IEnumerable<int[]> vRange = from x in Enumerable.Range(0, size[0])
                         from y in Enumerable.Range(0, size[1])
                         from z in Enumerable.Range(0, size[2])
                         select new int[]{x, y, z};

            Parallel.ForEach(vRange, vec =>
             {
                 v[0] = vec[0];
                 v[1] = vec[1];
                 v[3] = vec[2];
             });
            return vRange as IEnumerable<int>;
        }
    

        public static int[] ProcessRange(int[] v, int x = 0, int y = 0, int z = 0, int rangeMin = 0, int rangeMax = 0)
        {

            IEnumerable<int[]> vRange = from X in Enumerable.Range(rangeMin, rangeMax)
                         from Y in Enumerable.Range(rangeMin, rangeMax)
                         from Z in Enumerable.Range(rangeMin, rangeMax)
                         select new int[]{x, y, z};

            Parallel.ForEach(vRange, vec =>
            {
                v[0] = vec[0];
                v[1] = vec[1];
                v[2] = vec[2];
            });
            return v;
            
        }
         public static int[] ProcessRange(int[] v, int x = 0, int y = 0, int z = 0, int sizeX = 0, int sizeY = 0, int sizeZ = 0)
        {

            IEnumerable<int[]> vRange = from X in Enumerable.Range(0, sizeX)
                         from Y in Enumerable.Range(0, sizeY)
                         from Z in Enumerable.Range(0, sizeZ)
                         select new int[]{x, y, z};

            Parallel.ForEach(vRange, vec =>
            {
                v[0] = vec[0];
                v[1] = vec[1];
                v[2] = vec[2];
            });
            return v;
            
        }
        

    }    
}