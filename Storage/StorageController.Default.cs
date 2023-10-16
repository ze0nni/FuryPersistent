using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fury.Storage
{
    public sealed partial class StorageController
    {
        static readonly Dictionary<Type, StorageController> _controllers
            = new Dictionary<Type, StorageController>();

        public static StorageController Get(string userId, Type storageType)
        {
            if (!_controllers.TryGetValue(storageType, out var controller))
            {
                controller = new StorageController(
                    storageType, 
                    CreateContainers(storageType));
                _controllers.Add(storageType, controller);
            }
            return controller;
        }

        static IEnumerable<IStorageContainer> CreateContainers(Type storageType)
        {
            foreach (var field in storageType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                var t = field.FieldType;
                if (t.IsGenericType)
                {
                    var gt = t.GetGenericTypeDefinition();
                    if (gt == typeof(Singletone<>))
                    {
                        var itemType = t.GetGenericArguments()[0];
                        var c = (IStorageContainer)Activator.CreateInstance(typeof(Singletone<>).MakeGenericType(itemType), field.Name);
                        field.SetValue(null, c);
                        yield return c;
                    }
                    else if (gt == typeof(StorageList<>) || gt == typeof(IList<>))
                    {
                        var itemType = t.GetGenericArguments()[0];
                        var c = (IStorageContainer)Activator.CreateInstance(typeof(StorageList<>).MakeGenericType(itemType), field.Name);
                        field.SetValue(null, c);
                        yield return c;
                    }
                    else if (gt == typeof(StorageDictionary<,>) || gt == typeof(IDictionary<,>))
                    {
                        var keyType = t.GetGenericArguments()[0];
                        var itemType = t.GetGenericArguments()[1];
                        var c = (IStorageContainer)Activator.CreateInstance(typeof(StorageDictionary<,>).MakeGenericType(keyType, itemType), field.Name);
                        field.SetValue(null, c);
                        yield return c;
                    }
                }
            }
        }
    }
}