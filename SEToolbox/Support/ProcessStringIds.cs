
using System;

using VRage.Utils;

namespace SEToolbox.Support
{

    public static class ProcessStringIds
    {

        private static readonly TwoWayDictionary<string, MyStringId> ProcessedIds = new();

        public static MyStringId? ProcessIds(string str)
        {
              
            
            if (string.IsNullOrEmpty(str))
            {
                var nullValue = default(MyStringId);
                return ProcessedIds.TryGetValue("", nullValue) ? nullValue : CreateMyStringId(0);
            }
                var value = default(MyStringId);
            if (!ProcessedIds.TryGetValue(str, value))
            {
                value = CreateMyStringId(ProcessedIds.Count);
                ProcessedIds.Add(str, value);
            }

            return (MyStringId?)value;
        }
        
        private static MyStringId CreateMyStringId(int id)
            => (MyStringId)Activator.CreateInstance(typeof(MyStringId), id);
        



        //was a test method that endedd up too complicated

        // public static Tuple<string, MyStringId?> FlipStringId(string str, MyStringId? value)
        // {

        //     // If both are null → return empty
        //     if (string.IsNullOrEmpty(str) && value == null)
        //         return new Tuple<string, MyStringId?>(null, null);

        //
        //     if (!string.IsNullOrEmpty(str))
        //     {
        //         if (FlipStringIds.TryGetValue(str, out var existingId))
        //             return new Tuple<string, MyStringId?>(str, existingId);

        //         var newId = CreateMyStringId(FlipStringIds.Count);
        //         FlipStringIds.Add(str, newId);

        //         if (!FlipStringIds.TryGetValue(str, out _))
        //             return new Tuple<string, MyStringId?>(str, null); // Fallback (should never hit here;

        //     // If we have an ID → reverse lookup
        //     // if (value!= null)
        //     // {
        //     //     if (FlipStringIds.TryGetValue(value, out var existingStr))
        //     //         return new MyTuple<string, MyStringId?>(existingStr, value);

        //     //     return new MyTuple<string, MyStringId?>(null, value);
        //     // }

        //     // Fallback (should never hit here)
        //     return new Tuple<string, MyStringId?>(str, value);
    }
}

