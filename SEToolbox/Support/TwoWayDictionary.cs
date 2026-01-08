
using System.Collections.Concurrent;
using System.Linq;


namespace SEToolbox.Support
{

    public class TwoWayDictionary<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _forward;
        private readonly ConcurrentDictionary<TValue , TKey> _reverse;

        public TwoWayDictionary()
        {
            _forward = new ConcurrentDictionary<TKey, TValue>();
            _reverse = new ConcurrentDictionary<TValue, TKey>();
        }


        public bool TryGetValue(object into, object outOf)
        {   
            return (into, outOf) switch
            {
                TKey key when outOf is TValue => _forward.TryGetValue(key, out _),
                TValue value when outOf is TKey => _reverse.TryGetValue(value, out _),
                _ => false,
            };
        }

        public bool TryGetValues(object into, object outOf)
        {
            foreach (var kvp in _forward)
            {
                return (into, outOf) switch
                {
                    TKey key when outOf is TValue => _forward.TryGetValue(key, out _),
                    TValue value when outOf is TKey => _reverse.TryGetValue(value, out _),
                    _ => false,
                };
            }

            return false;
        }

        public void Add(object key, object value)
        {
            if (key is TKey k && value is TValue v)
            {
                if (!_forward.TryAdd(k, v))
                {
                    return;
                }

                _reverse.TryAdd(v, k);
            }
        }
        
        public void Remove(TKey key) {
            _forward.TryRemove(key, out _);
            _reverse.TryRemove(_forward[key], out _);
        }

        public int Count => _forward.Count;

    }

}