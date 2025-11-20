using System;
using System.Text;
using System.Threading;

namespace SEToolbox.Support
{
    public static class RandomUtil
    {
        static readonly Guid _guid = Guid.NewGuid();

        static int _seed;
        static readonly int _secretSeed = BitConverter.ToInt32(BitConverter.GetBytes(DateTime.Now.Ticks), 0) ^ BitConverter.ToInt32(Encoding.UTF8.GetBytes(_guid.ToString()), 0);

        static readonly ThreadLocal<Random> _threadLocalRandom = new(() => new Random(_seed));

        public static Random MyRandom
        {

            get => _threadLocalRandom.Value;
            set => _threadLocalRandom.Value = value;
        }

        public static bool EnableSecretRandom
        {   
            get => _threadLocalRandom.IsValueCreated;
            set => _threadLocalRandom.Value = value ? new Random(_secretSeed)  : null;
        }
      
        public static void SetSeed(int? seed = null,int? value = null, bool isSecretRandom = false)
        {
            if (seed.HasValue && seed.Value != 0)
            {
                if (_threadLocalRandom.IsValueCreated)
                    _threadLocalRandom.Value = new Random(seed.Value);
                if (!seed.HasValue)
                    _threadLocalRandom.Value = new Random(_seed);
                if (isSecretRandom)
                   EnableSecretRandom = true;
            }
            if (seed.HasValue)
            {
               _seed = seed.Value;
            }
        }
  
 
      


        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <param name="maxValue"> The exclusive upper bound of the random number to be generated. maxValue must be greater than or equal to zero.</param>
        /// <returns></returns>
        public static int GetInt(int maxValue)
        {
            return MyRandom.Next(maxValue);
        }

        /// <summary>
        /// Returns a random number within a specified range.
        /// </summary>
        /// <param name="minValue">minValue is the inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">maxValue is the exclusive upper bound of the random number returned.</param>
        /// <returns></returns>
        public static int GetInt(int minValue, int maxValue)
        {
            return MyRandom.Next(minValue, maxValue);
        }

        /// <summary>
        /// Returns a random number within a specified range.
        /// </summary>
        /// <param name="minValue">minValue is the inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">maxValue is the exclusive upper bound of the random number returned.</param>
        /// <returns></returns>
        public static float GetRandomFloat(float minValue, float maxValue)
        {
            return (float)MyRandom.NextDouble() * (maxValue - minValue) + minValue;
        }

        /// <summary>
        /// Returns a random number within a specified range.
        /// </summary>
        /// <param name="minValue">minValue is the inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">maxValue is the exclusive upper bound of the random number returned.</param>
        /// <returns></returns>
        public static double GetDouble(double minValue, double maxValue)
        {
            return MyRandom.NextDouble() * (maxValue - minValue) + minValue;
        }
    }
}
