using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SEToolbox.Support
{
    public static class Conditional
    {


        public static bool All(params object[] values) => values.All(v => v != values[0]);

        public static bool All(IEnumerable<object> values, object condition = null) => values.All(v => v != condition);

        //public static bool Any<T>(this IEnumerable<T> values) => values.Any( v => v != null);
        public static bool Any(params object[] values) => values.Any(v => v != null);
        public static bool AllNull(params object[] values) => values.All(v => v == null);

        public static bool AnyOf<T>(Func<T, bool> predicate, params T[] values)
        {
            foreach (var value in values)
            {
                if (predicate(value))
                    return true;
            }
            return false;
        }
        public static bool NotNull(params object[] values) => !(bool)Condition(null, values);
        public static bool NotNullOrDefault<T>(params object[] values) => !(bool)Condition((object)null ?? default, values);
        public static bool Null(params object[] values) => (bool)Condition(null, values);
        public static bool Is(params object[] values) => (bool)Condition(true, values);
        public static bool IsNot(params object[] values) => (bool)Condition(false, values);
        public static bool Equals(params object[] values) => (bool)Condition(values[0], values);

        /// <summary>
        /// Returns true if the condition matches any of the values.
        /// If the condition is null, returns true if any of the values are not null.
        /// </summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="values">The values to check.</param>
        /// <returns>true if the condition matches any of the values; otherwise, false.</returns>
        public static object Condition(object condition = null, params object[] values)
        {
            if (condition != null)
            {
                var conditionType = condition.GetType();
                var valueTypes = values.Select(v => v?.GetType()).ToArray();
                var firstValueType = valueTypes.FirstOrDefault();

                // Check if the condition is of the same type as any of the values, or if it is assignable from any of the values, or if it is an instance of any of the values.
                return condition is char c || 
                       condition is string s ||
                       conditionType.IsInstanceOfType(condition) ||
                       ReferenceEquals(condition, firstValueType) || 
                       conditionType.IsAssignableFrom(firstValueType) ||
                       conditionType.IsInstanceOfType(firstValueType) ||
                       valueTypes.Any(t => conditionType.IsAssignableFrom(t) ||
                                           conditionType.IsInstanceOfType(t));
            }

            // If the condition is null, return true if all of the values are not null.
            return values.All(v => v != null);
        }

        /// <summary>
        /// Returns true if the condition matches any of the values in the given condition-value pairs.
        /// </summary>
        /// <param name="conditionPairs">The condition-value pairs.</param>
        /// <returns>true if the condition matches any of the values; otherwise, null.</returns>
        public static object ConditionPairs(params object[] conditionPairs)
        {
            Dictionary<object, object> valuePairs = [];
            if (conditionPairs.Length == 0 || conditionPairs.All(v => v == null) || valuePairs.Count == 0)
                return null;

            object condition = null, value = null;
            foreach (var pair in conditionPairs)
            {
                var conditionValuePair = pair;
                    conditionValuePair = (object c, object v) => 
                    valuePairs.Add(c = condition, v = value);
                if (condition != null && Equals(condition, value))
                {
                    return value ?? true;
                }
            }
            return valuePairs.All(v => !v.Value.Equals(condition)) ? null : true;
        }

        public static object NullCoalesced(params object[] values) => ConditionCoalesced(null, values);
        /// <summary>
        /// Conditionally coalesce values based on the condition and value.
        /// If the condition matches any of the values, return the value.
        /// Otherwise, return the swap or null if the swap is null.
        /// </summary>
        public static object ConditionCoalesced(object condition, object value, object swap = null, params object[] values)
        {
            var coalesced = (bool)Condition(condition, value, swap) ? value : swap;
            var conditionMatchesValues = (bool)Condition(condition, values);
            if (Null(condition, value, swap, values))
                return null;

            if (values.Length == 0)
                return coalesced;

            return conditionMatchesValues ? null : value ?? swap ?? condition ?? Condition(condition, values);
        }

    }
}


