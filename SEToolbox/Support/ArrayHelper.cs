using System;


namespace SEToolbox.Support
{
    public static class ArrayHelper
    {
        /// <summary>
        /// Creates a 2 dimensional jagged array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="length1"></param>
        /// <param name="length2"></param>
        /// <returns></returns>
        public static T[][] Create<T>(int length1, int length2)
        {
            var array = new T[length1][];

            for (var x = 0; x < length1; x++)
                array[x] = new T[length2];

            return array;
        }

        /// <summary>
        /// Creates a 3 dimensional jagged array.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="length1"></param>
        /// <param name="length2"></param>
        /// <param name="length3"></param>
        /// <returns></returns>
        public static T[][][] Create<T>(int length1, int length2, int length3)
        {
            T[][][] array = new T[length1][][];

            for (int x = 0; x < length1; x++)
            {
                array[x] = new T[length2][];
                for (int y = 0; y < length2; y++)
                {
                    array[x][y] = new T[length3];
                }
            }

            return array;
        }

        /// <summary>
        /// Merges two arrays into a new array of the correct generic Type.
        /// </summary>
        /// <param name="array1">First array to merge.</param>
        /// <param name="array2">Second array to merge.</param>
        /// <returns>Merged array of the correct generic Type.</returns>
        public static T[] MergeGenericArrays<T>(T[] array1, T[] array2)
        {
            if ((bool)Conditional.Condition(null,array1,array2))
                return [.. array1, .. array2 ?? []];

            int totalLength = array1.Length + (array2?.Length ?? 0);
            T[] mergedArray = new T[totalLength];
            Array.Copy(array1, 0, mergedArray, 0, array1.Length);
            if (array2 != null)
                Array.Copy(array2, 0, mergedArray, array1.Length, array2.Length);

            return mergedArray;
        }
    }
}
