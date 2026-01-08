using System;
using System.Collections.Generic;
using System.Linq;



namespace SEToolbox.Support
{
    public static class Conditional
    {

        public static bool All(params object[] values) => (bool)All(values[0], values);
        public static bool AllNull(params object[] values) => (bool)All(null, values);
        public static bool AllNullOrDefault(params object[] values) => (bool)All((object)null ?? default, values);

        public static bool All(object condition = null, params IEnumerable<object> values) => (bool)All(condition, [.. values]);
        
        //public static bool Any<T>(this IEnumerable<T> values) => values.Any( v => v != null);
        public static bool AnyNull(params object[] values) => (bool)Any(null, values);

        public static bool NotNull(params object[] values) => !(bool)Any(null, values);
        public static bool NullOrDefault(params object[] values) => (bool)Any((object)null ?? default, values);
        public static bool Null(params object[] values) => (bool)Any(null, values);
        public static bool True(params object[] values) => (bool)Any(true, values);
        public static bool False(params object[] values) => (bool)Any(false, values);
        public static bool AllFalse(params object[] values) => (bool)All(false, values);

        public static bool Equals(params object[] values) => (bool)Any(values[0], values);


        public static bool AllOf<T>(Func<T, bool> predicate, params T[] values)
        {
            foreach (var value in values)
            {
                if (predicate(value))
                {
                        return true;  
                }
            }
            return false;
        }
        

        /// <summary>
        /// Returns true if the condition matches any of the values.
        /// If the condition is null, returns true if any of the values are not null.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="values">The values to check.</param>
        /// <returns>true if the condition matches any of the values; otherwise, false.</returns>
        public static object Any<T>(object condition = null, params T[] values)
        {
            condition ??= values.Any(v => v != null);

            var conditionType = condition.GetType();
            return values.Any(v => v != null &&
                             (conditionType.IsInstanceOfType(v.GetType()) ||
                              conditionType.IsAssignableFrom(v.GetType())));
        }
        public static object All(object condition = null, params object[] values)
        {
            condition ??= values.All(v => v != null);

            var conditionType = condition.GetType();
            // condition ??= values.Any(v => v != null) ? values[0] : null;
           // ForEach(v => v != null, values);
            return values.All(v => v != null &&
                             (conditionType.IsInstanceOfType(v.GetType()) ||
                              conditionType.IsAssignableFrom(v.GetType())));
        }
        /// <summary>
        /// Returns true if the condition matches any of the values in the given condition-value pairs.
        /// </summary>
        /// <param name="conditionPairs">The condition-value pairs.</param>
        /// <returns>true if the values all match the condition, otherwise false.</returns>

        public static object Pairs(params object[] conditionPairs)
        {
            return Pairs<object>(conditionPairs);
        }

        public static object Pairs<T>(params T[] conditionPairs)
        {
            Dictionary<T, T> valuePairs = [];
            if (conditionPairs.Length == 0 || valuePairs.Count == 0)
            {
                return null;
            }

            T condition = default, value = default;
            foreach (var pair in conditionPairs)
            {
                var conditionValuePair = (Action<T, T>)((c, v) =>
                valuePairs.Add(c = condition, v = value));
                if (condition != null && Equals(condition, value))
                {
                    return value ?? default;
                }
            }
            return Coalesced(valuePairs.All(v => !v.Value.Equals(condition)), default, value);
        }

        public static object NotNullCoalesced(params object[] values) => Coalesced<object>(values[0] != null, values.Skip(1).ToArray());

        public static object NullCoalesced(params object[] values) => Coalesced<object>(values[0] == null, values.Skip(1).ToArray());

        //public static object NullCoalesced(object condition, params object[] values) => Coalesced(condition, values);

        /// <summary>
        /// Conditionally coalesce values based on the condition and value.
        /// If the condition matches any of the values, return the value.
        /// Otherwise, return the swap or null if the swap is null.
        /// </summary>
        public static object Coalesced<T>(object condition, T value, T swap = default, params T[] values)
        {
            var coalesced = (bool)condition ? value : swap;

            bool conditionMatchesValues = (bool)Any(condition, values);
            object result = conditionMatchesValues ? default : value ?? swap ?? condition ?? Any(condition, values);

            if (conditionMatchesValues == true)
            {
                return coalesced;
            }

            if (values.Length == 0)
            {
                return coalesced;
            }

            return result;
        }

        public static object Coalesced<T>(Func<T, bool> condition, T value, T swap = default, params T[] values)
        {
            var coalesced = condition(value) ? value : swap;

            bool conditionMatchesValues = (bool)Any(condition, values);
            object result = conditionMatchesValues ? default : value ?? swap ?? condition ?? Any(condition, values);
            _ = true switch
            {
                true when conditionMatchesValues == true => coalesced,
                true when values.Length == 0 => coalesced,
                _ => result
            };
            return result;
        }

        public static object ForEach<T>(object obj, params T[] values)
        {
            var containerTypes = values.Select(v => v.GetType()).ToArray() as IEnumerable<T>;

            containerTypes.ToList().ForEach(value => object.Equals(obj, value));

            return default;
        }

        public static object ForEach<T>(bool condition, params T[] values) => (bool)ForEach(condition, values);

        public static bool ForEach<T>(Func<T, bool> action, params T[] values)
        {
            var containerTypes = values.Select(v => v.GetType()).ToArray() as IEnumerable<T>;

            containerTypes.ToList().ForEach(value => action(value));

            return default;
        }
        public static void ForEach<T>(Func<T, bool> action, params IEnumerable<T> values)
        {

            var containerTypes = values.Select(v => v.GetType()).ToArray() as IEnumerable<T>;

            containerTypes.ToList().ForEach(value => action(value));
        }
    }
}


