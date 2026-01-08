using System;
using System.Text;
using System.Threading;

namespace SEToolbox.Support
{
    public static class RandomUtil
    {
        static readonly Guid _guid = Guid.NewGuid();
        static int _seed = 0;
        static readonly int _secretSeed = BitConverter.ToInt32(BitConverter.GetBytes(DateTime.Now.Ticks), 0) ^ BitConverter.ToInt32(_guid.ToByteArray(), 0);
        static readonly ThreadLocal<Random> _threadLocalRandom = new(() => new Random(Interlocked.Increment(ref _seed)));

        public static Random ThreadLocalRandom
        {
            get => _threadLocalRandom.Value ;
            set => _threadLocalRandom.Value = value;
        }

        public static bool EnableSecretRandom
        {
            get => _threadLocalRandom.IsValueCreated;
            set => _threadLocalRandom.Value = value ? new Random(_secretSeed) : null;
        }

        //to not break existing code
        static Random MyRandom => ThreadLocalRandom;

        public static int SetSeed(int? seed = null, int? value = null)
        {
            var newSeed = seed ?? _seed;
            newSeed = value ?? newSeed;

            if (_threadLocalRandom.IsValueCreated)
            {
                _threadLocalRandom.Value = new Random(newSeed);
            }
            else
            {
                Interlocked.Exchange(ref _seed, newSeed);
            }
            return newSeed;
        }
        // intended for Asteroids primarily   
        // todo maybe for new empty planets possible in the future) 
        public static int GetSeed() => _threadLocalRandom.IsValueCreated ? _threadLocalRandom.Value.Next() : _seed;

        public static void SetSecretRandom(int seed, bool? superSecret = false)
        {
            _threadLocalRandom.Value = new Random(seed ^ _secretSeed);
            if (superSecret.HasValue && superSecret.Value)
            {

                if (_threadLocalRandom.IsValueCreated && _threadLocalRandom.Value.GetHashCode() == new Random(_secretSeed).GetHashCode())
                {
                    return;
                }
                _threadLocalRandom.Value = new Random(_secretSeed);
            }
        }
        // TODO: bury the super secret seed option somewhere fun in the code

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
