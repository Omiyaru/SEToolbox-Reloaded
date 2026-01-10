using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using VRageMath;

namespace SEToolbox.Support
{
    public static class PRange
    {
        public static object ProcessRange(object v, object size) => ProcessRange(v, size);
        
        public static IEnumerable<T> ProcessRange<T>(IEnumerable<T> v, IEnumerable<T> size) where T : struct
        {
            var sizeType = size.GetType();
            var sizeList = size as IEnumerable<ImmutableArray<T>>;
            var vRange = from x in Enumerable.Range(0, sizeList.First().Count())
                         from y in Enumerable.Range(0, sizeList.Skip(1).First().Count())
                         from z in Enumerable.Range(0, sizeList.Skip(2).First().Count())
                         select new { x, y, z };
            if (!Conditional.Equals(typeof(T), v.GetType(), vRange))
            {
                var vRangeConv = vRange.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToArray();
                v = (T[])Array.CreateInstance(v.GetType().GetElementType(), vRangeConv.Count());
                var vIndex = 0;
                Parallel.ForEach(vRangeConv, vec =>
                {
                    var vArray = v.ToArray();
                    vArray[vIndex] = (T)(object)vec;
                    Interlocked.Increment(ref vIndex);
                });

            }
            return v;
        }

        public static IEnumerable<T> ProcessRange<T>(T x, T y, T z, int rangeMin = 0, int rangeMax = 0) where T : struct
        {
            var v = new[] { x, y, z };
            var vRange = from X in Enumerable.Range(rangeMin, rangeMax)
                         from Y in Enumerable.Range(rangeMin, rangeMax)
                         from Z in Enumerable.Range(rangeMin, rangeMax)
                         select new { x, y, z };

            if (!Conditional.Equals(typeof(IEnumerable<T>), v.GetType(), vRange))
            {
                var vRangeConv = vRange.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToArray();
                v = (T[])Array.CreateInstance(v.GetType().GetElementType(), vRangeConv.Length);
                var vIndex = 0;
                Parallel.ForEach(vRangeConv, vec =>
                {
                    var vArray = v.ToArray();
                    vArray[vIndex] = vec;
                    Interlocked.Increment(ref vIndex);
                });
            }
            return v;
        }
       
        public static IEnumerable<T> ProcessRange<T>(IEnumerable<T> v, T x, T y, T z, int rangeMin = 0, int rangeMax = 0) where T : struct
        {
            var vRange = from X in Enumerable.Range(rangeMin, rangeMax)
                         from Y in Enumerable.Range(rangeMin, rangeMax)
                         from Z in Enumerable.Range(rangeMin, rangeMax)
                         select new { x, y, z };

            if (!Conditional.Equals(typeof(IEnumerable<T>), v.GetType(), vRange))
            {
                var vRangeConv = vRange.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToArray();
                v = (T[])Array.CreateInstance(v.GetType().GetElementType(), vRangeConv.Length);
                var vIndex = 0;
                Parallel.ForEach(vRangeConv, vec =>
                {
                    var vArray = v.ToArray();
                    vArray[vIndex] = vec;
                    Interlocked.Increment(ref vIndex);
                });
            }
            return v;
        }


        public static IEnumerable<T> ProcessRange<T>(IEnumerable<T> v, int rangeMin, int rangeMax) where T : struct
        {

            var vRange = from X in Enumerable.Range(rangeMin, rangeMax)
                         from Y in Enumerable.Range(rangeMin, rangeMax)
                         from Z in Enumerable.Range(rangeMin, rangeMax)
                         select new { X, Y, Z };

            if (!Conditional.Equals(typeof(T), v.GetType(), vRange))
            {
                var vRangeConv = vRange.SelectMany(vec => new[] { vec.X, vec.Y, vec.Z }).ToArray();
                v = (T[])Array.CreateInstance(v.GetType().GetElementType(), vRangeConv.Count());
                var vIndex = 0;
                Parallel.ForEach(vRangeConv, vec =>
                {
                    var vArray = v.ToArray();
                    vArray[vIndex] = (T)(object)vec;
                    Interlocked.Increment(ref vIndex);
                });
            }

            return v;

        }
        public static IEnumerable<T> ProcessRange<T>(IEnumerable<T> v, int x, int y, int z, int sizeX = 0, int sizeY = 0, int sizeZ = 0) => ProcessRange<T>(v, x, y, z, sizeX, sizeY, sizeZ);
        public static IEnumerable<T> ProcessRange<T>(IEnumerable<T> v, T x, T y, T z, int sizeX = 0, int sizeY = 0, int sizeZ = 0)
        {
            var vRange = from X in Enumerable.Range(0, sizeX)
                         from Y in Enumerable.Range(0, sizeY)
                         from Z in Enumerable.Range(0, sizeZ)
                         select new { x, y, z };

            if (!Conditional.Equals(typeof(IEnumerable<T>), v.GetType(), vRange))
            {
                var vRangeConv = vRange.SelectMany(vec => new[] { vec.x, vec.y, vec.z }).ToArray();
                v = (IEnumerable<T>)Array.CreateInstance(v.GetType().GetElementType(), vRangeConv.Length);
                var vIndex = 0;
                Parallel.ForEach(vRangeConv, vec =>
                {
                    var vArray = v.ToArray();
                    vArray[vIndex] = vec;
                    Interlocked.Increment(ref vIndex);
                });
            }
            return v;
        }
    }
}