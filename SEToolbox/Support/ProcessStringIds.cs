
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
        {
            return (MyStringId)Activator.CreateInstance(typeof(MyStringId), id);
        }
    }
}

