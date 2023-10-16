using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fury.Storage
{
    internal interface IStorageContainer
    {
        void Reload(StorageController controller);
    }

    public sealed partial class StorageController
    {
        public readonly Type StorageType;

        readonly IStorageContainer[] _containers;

        KeysObserver _keys;
        public KeysObserver Keys => _keys;

        readonly StorageMediator _mediator;

        internal StorageController(
            Type storageType,
            IEnumerable<IStorageContainer> containers)
        {
            StorageType = storageType;
            _containers = containers.ToArray();

            var go = new GameObject($"[{nameof(StorageController)}:{storageType.Name}]");
            GameObject.DontDestroyOnLoad(go);
            _mediator = go.AddComponent<StorageMediator>();
            _mediator.Register(this);

            Reload();
        }

        internal void Reload()
        {
            //TODO: _keys.Wait();

            _keys?.Dispose();
            _keys = new KeysObserver();
            foreach (var c in _containers)
            {
                c.Reload(this);
            }
        }

        internal bool Get<T>(string id, out T value)
        {
            value = default;
            return false;
        }
    }
}