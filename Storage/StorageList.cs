using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fury.Storage
{
    public sealed class StorageList<TValue> : IStorageContainer, IList<TValue>
        where TValue : class
    {
        readonly string _id;
        string NewItemGuid() => $"{_id}:{Guid.NewGuid().ToString()}";
        void UpdateGuidsKey() => _controller.Keys.Update(_id, _origin.Select(i => i.guid).ToArray());

        readonly List<(string guid, TValue data)> _origin = new List<(string guid, TValue data)>();

        public StorageList(string id)
        {
            _id = id;
        }

        StorageController _controller;

        public void Reload(StorageController controller)
        {
            _controller = controller;
            _origin.Clear();

            if (controller.Get<string[]>(_id, out var guids))
            {
                foreach (var guid in guids)
                {
                    if (controller.Get<TValue>(guid, out var item))
                    {
                        _origin.Add((guid, item));
                        _controller.Keys.Update(guid, item);
                    }
                }
            }
            UpdateGuidsKey();
        }

        public TValue this[int index] {
            get {
                var (guid, data) = _origin[index];
                _controller.Keys.MarkDirty(guid);
                return data;
            }
            set
            {
                var guid = _origin[index].guid;
                _origin[index] = (guid, value);
                _controller.Keys.Update(guid, value);
            }
        }

        public int Count => _origin.Count;

        public bool IsReadOnly => false;

        public void Add(TValue data)
        {
            Insert(Count, data);
        }

        public void Clear()
        {
            foreach (var (guid, _) in _origin)
            {
                _controller.Keys.Delete(guid);
            }
            _origin.Clear();
        }

        public bool Contains(TValue data)
        {
            foreach (var (_, d) in _origin)
            {
                if (data == d)
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<TValue> GetEnumerator()
        {
            foreach (var item in _origin)
            {
                _controller.Keys.MarkDirty(item.guid);
                yield return item.data;
            }
        }

        public int IndexOf(TValue data)
        {
            for (var i = 0; i < _origin.Count; i++)
            {
                if (_origin[i].data == data)
                {
                    return i;
                }
            }
            return -1;
        }

        public void Insert(int index, TValue data)
        {
            var item = (guid: NewItemGuid(), data: data);
            _origin.Insert(index, item);
            _controller.Keys.Update(item.guid, item.data);
            UpdateGuidsKey();
        }

        public bool Remove(TValue data)
        {
            var index = IndexOf(data);
            if (index == -1)
            {
                return false;
            }
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            var item = _origin[index];
            _origin.RemoveAt(index);
            _controller.Keys.Delete(item.guid);
            UpdateGuidsKey();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var item in _origin)
            {
                _controller.Keys.MarkDirty(item.guid);
                yield return item.data;
            }
        }
    }
}