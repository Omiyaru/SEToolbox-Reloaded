using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SEToolbox.Support
{
    public static class Conditional
    {


        public static bool All(params object[] values)
        {
            return values.All(v => v != values[0]);
        }

        public static bool All(IEnumerable<object> values, object condition = null)
        {
            return values.All(v => v != condition);
        }

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
        public static bool ConditionNull(params object[] values) => (bool)Condition(null, values);
        public static bool ConditionIs(params object[] values) => (bool)Condition(true, values);
        public static bool ConditionNot(params object[] values) => (bool)Condition(false, values);

     public static object ConditionChar(object condition, params object[] values)
        {
            var result = (char)Condition(condition, values);
            condition =   condition as char? ?? condition;
            values = [.. values.Where(v => v is char c)];
            var value = values.FirstOrDefault();
            return result;
        }
        public static  object Condition(object condition = null, params object[] values) 
        {
            if (condition != null)
            {
                var conditionType = condition.GetType();
                var valueTypes = values.Select(v => v?.GetType()).ToArray();
                var firstValueType = valueTypes.FirstOrDefault();
           
                return  conditionType.IsInstanceOfType(condition) || condition is char c  ||
                    conditionType.IsAssignableFrom(firstValueType) || conditionType.IsInstanceOfType(firstValueType) ||
                       valueTypes.Any(t => conditionType.IsAssignableFrom(t) || conditionType.IsInstanceOfType(t)) ||
                       ReferenceEquals(condition, firstValueType);

            }

            return values.Any(v => v != null);
        }
        public static object ConditionPairs(params object[] conditionPairs)
        {
            if (conditionPairs.Length == 0 || conditionPairs.All(v => v == null))
                return null;

            Dictionary<object, object> valuePairs = [];
            object condition = null, value = null;
            foreach (var pair in conditionPairs)
            {
                var conditionPair = pair;
                conditionPair = (object c, object v) => valuePairs.Add(c = condition, v = value);


                if (!ReferenceEquals(condition,null) && condition.Equals(value))
                {
                    return value ?? true;
                }
                return false;
            }
               return null;
        }
      
        public static object NullCoalesced(params object[] values) => ConditionCoalesced(null, values);

        public static object ConditionCharCoalesced(char condition, char value, char swap = default, params char[] values) => (char)ConditionCoalesced(condition, value, swap, values);
        public static object ConditionCoalesced(object condition, object value, object swap = null, params object[] values)
        {
            var coalesced = (bool)Condition(condition, value, swap) ? value : swap;
            var conditionMatchesValues = Condition(condition, values);

            if (ConditionNull(condition, value, swap, values))
                return null;

            if (values.Length == 0)
                return coalesced;


            return (bool)conditionMatchesValues ? null : value ?? swap ?? condition ?? Condition(condition, values);
        }

        public static bool AssignableFrom<T>(object condition = null, params T[] values)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition), $"{nameof(condition)} cannot be null.");

            foreach (var value in values)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(values), $"Value {values.ToList().IndexOf(value)} is null.");

                if (!condition.GetType().IsAssignableFrom(value.GetType()))
                    throw new ArgumentException(nameof(values), $"Value {values.ToList().IndexOf(value)} of type {value.GetType()} is not assignable from type {condition.GetType()}.");
            }
            return true;
        }

        public static bool InstanceOfType(object condition = null, params object[] values)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition), $"{nameof(condition)} cannot be null.");

            foreach (var value in values)
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(values), $"Value {values.ToList().IndexOf(value)} is null.");

                if (!condition.GetType().IsInstanceOfType(value.GetType()))
                    throw new ArgumentException(nameof(values), $"Value {values.ToList().IndexOf(value)} of type {value.GetType()} is not an instance of type {condition.GetType()}.");
            }
            return true;
        }

        public static IEnumerable Where(params object[] values) => values.Where(v => v != null);
        public static IEnumerable Where<T>(T value, params Func<object, T>[] predicate) => predicate.Where(v => value != null);
    }
}


