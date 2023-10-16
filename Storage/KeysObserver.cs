using System;
using System.Collections.Generic;

namespace Fury.Storage
{
    public class KeyState
    {
        public readonly string Id;
        public KeyState(string id) => Id = id;

        public object Data { get; private set; }

        public bool IsDirty { get; private set; }
        public int StoredHash { get; private set; }
        public int CurrentHash { get; private set; }
        public bool IsStored => !IsDirty && StoredHash == CurrentHash;

        internal void Update(object data)
        {
            Data = data;
            IsDirty = true;
            StoredHash = 0;
            CurrentHash = 0;
        }

        internal void MarkDirty()
        {
            IsDirty = true;
        }
    }

    public sealed class KeysObserver : IDisposable
    {
        readonly SortedDictionary<string, KeyState> _keys
            = new SortedDictionary<string, KeyState>();
        readonly HashSet<string> _removed
            = new HashSet<string>();

        public IEnumerable<KeyState> List => _keys.Values;

        public void Update(string id, object value)
        {
            _removed.Remove(id);
            if (!_keys.TryGetValue(id, out var state))
            {
                state = new KeyState(id);
                _keys.Add(id, state);
            }
            state.Update(value);
        }

        public void Delete(string id)
        {
            _removed.Add(id);
            _keys.Remove(id);
        }

        public void MarkDirty(string id)
        {
            if (_keys.TryGetValue(id, out var state))
            {
                state.MarkDirty();
            }
        }

        public void Dispose()
        {
            
        }
    }
}
