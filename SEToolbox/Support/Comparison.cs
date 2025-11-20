using System;
using System.Collections.Generic;
using System.Linq;

namespace SEToolbox.Support
{
    public class Comparison<T> where T : IEquatable<T>, IComparable<T>
    {
        public T Value { get; }
        
        public T[] Values { get; }
        
        public int Count => Values?.Length ?? 0;
        public T First => Values.FirstOrDefault();

        protected Comparison(T value) => Value = value;
        protected Comparison(params T[] values) => Values = values;
    }

    public class CompareAny<T> : Comparison<T> where T : IEquatable<T>, IComparable<T>
    {
        public CompareAny(T value) : base(value) { }
        
        public CompareAny(params T[] values) : base(values) { }

        public override bool Equals(object obj) => obj is CompareAny<T> other && Equals(Value, other.Value);
        public override int GetHashCode() => Value.GetHashCode();

        public static implicit operator CompareAny<T>(T value) => new(value);
        public static implicit operator T(CompareAny<T> comparison) => comparison.Value;
        public static implicit operator CompareAny<T>( T[] values) => new(values);

        public static bool operator <(CompareAny<T> a, T[] b) => b.Any(v => a.Value.CompareTo(v) < 0);
        public static bool operator >(CompareAny<T> a, T[] b) => b.Any(v => a.Value.CompareTo(v) > 0);
        public static bool operator <=(CompareAny<T> a, T[] b) => b.Any(v => a.Value.CompareTo(v) <= 0);
        public static bool operator >=(CompareAny<T> a, T[] b) => b.Any(v => a.Value.CompareTo(v) >= 0);
        public static bool operator ==(CompareAny<T> a, T[] b) => b.Any(v => a.Value.CompareTo(v) == 0);
        public static bool operator !=(CompareAny<T> a, T[] b) => b.Any(v => a.Value.CompareTo(v) != 0);

    }

    public class CompareAll<T> : Comparison<T> where T : IEquatable<T>, IComparable<T>
    {
        public CompareAll(T value) : base(value) { }
        
        public CompareAll(params T[] values) : base(values) { }

        public override bool Equals(object obj) => obj is CompareAll<T> other && Equals(Value, other.Value);
        public override int GetHashCode() => Value.GetHashCode();

        public static implicit operator CompareAll<T>(T value) => new(value);
        public static implicit operator T(CompareAll<T> comparison) => comparison.Value;
        public static bool operator <(CompareAll<T> a, T[] b) => b.All(v => a.Value.CompareTo(v) < 0);
        public static bool operator >(CompareAll<T> a, T[] b) => b.All(v => a.Value.CompareTo(v) > 0);
        public static bool operator <=(CompareAll<T> a,T[] b) => b.All(v => a.Value.CompareTo(v) <= 0);
        public static bool operator >=(CompareAll<T> a,T[] b) => b.All(v => a.Value.CompareTo(v) >= 0);
        public static bool operator ==(CompareAll<T> a, T[] b) => b.All(v => a.Value.CompareTo(v) == 0);
        public static bool operator !=(CompareAll<T> a, T[] b) => b.All(v => a.Value.CompareTo(v) != 0);
    }   

    public class CompareConditional<T> : Comparison<bool> where T : IComparable<T>
    {
     
        
        public CompareConditional(params bool[] values) : base(values) { }
        
        public CompareConditional(T value) : base(value.CompareTo(default) != 0) { }
        
        public CompareConditional(params T[] values) : base([.. values.Select(v => v.CompareTo(default) != 0)]) { }
    public CompareConditional(int count, params T[] values) : base([.. values.Select(v => v.CompareTo(default) != 0).Take(count)]) { }


        public override bool Equals(object obj) => obj is CompareConditional<T> other && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => Values.GetHashCode();

        public static implicit operator CompareConditional<T>(T value) => new(value);
        public static implicit operator bool[](CompareConditional<T> comparison) => [comparison.TrueCount > 0];

        public static bool operator true(CompareConditional<T> a) => a.TrueCount > 0;
        public static bool operator false(CompareConditional<T> a) => a.TrueCount == 0;
           public int TrueCount => Values.Count(v => v);
        public static bool AllTrue(params CompareConditional<T>[] comparisons) => comparisons.All(v => v.TrueCount > 0);
        public static bool AnyTrue(params CompareConditional<T>[] comparisons) => comparisons.Any(v => v.TrueCount > 0);
        public static bool AllFalse(params CompareConditional<T>[] comparisons) => comparisons.All(v => v.TrueCount == 0);
        public static bool AnyFalse(params CompareConditional<T>[] comparisons) => comparisons.Any(v => v.TrueCount == 0);

        public bool Equals(params bool[] other) => Values.SequenceEqual(other);
    }
}

