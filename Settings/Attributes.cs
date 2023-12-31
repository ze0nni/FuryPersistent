using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;

namespace Fury.Settings
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class SettingsKeyFactoryAttribute : Attribute
    {
        public readonly IReadOnlyList<Type> Factories;
        public SettingsKeyFactoryAttribute(params Type[] factories)
        {
            Factories = factories.ToArray();
        }

        internal static IReadOnlyList<ISettingsKeyFactory> Resolve(Type settingsType)
        {
            var attr = settingsType.GetCustomAttribute<SettingsKeyFactoryAttribute>();
            if (attr == null)
            {
                return new ISettingsKeyFactory[0];
            }
            return attr
                .Factories
                .Select(type =>
                {
                    if (!typeof(ISettingsKeyFactory).IsAssignableFrom(type))
                    {
                        Debug.LogWarning($"Type {type.FullName} not implements {typeof(ISettingsKeyFactory).Name}");
                        return null;
                    }
                    try
                    {
                        return (ISettingsKeyFactory)Activator.CreateInstance(type);
                    } catch (Exception exc)
                    {
                        Debug.LogError($"Exception when try to create instance of {type.FullName}");
                        Debug.LogException(exc);
                    }
                    return null;
                })
                .Where(x => x != null)
                .ToArray();

        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class OnApplySettingsAttribute : Attribute
    {
        internal static Action ResolveCallback(Type type)
        {
            List<Action> actions = new List<Action>();

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.GetCustomAttribute<OnApplySettingsAttribute>() == null)
                {
                    continue;
                }
                try
                {
                    var action = (Action)Delegate.CreateDelegate(typeof(Action), null, method);
                    actions.Add(action);
                }
                catch (Exception exc)
                {
                    Debug.LogError($"Incorrect method {type.FullName}:{method.Name}. Except static method without arguments");
                    Debug.LogException(exc);
                }
            }

            if (actions == null || actions.Count == 0)
            {
                return null;
            }

            return () =>
            {
                foreach (var a in actions)
                {
                    try
                    {
                        a.Invoke();
                    }
                    catch (Exception exc)
                    {
                        Debug.LogError("Error when apply settings changes");
                        Debug.LogException(exc);
                    }
                }
            };
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SettingsStorageAttribute : Attribute
    {
        public const string DefaultFileName = "settings_{0}.json";

        public readonly StorageType StorageType = 0;
        public readonly string FileName;
        public readonly Type CustomStorageType;

        public SettingsStorageAttribute(StorageType storageType, string filename = DefaultFileName)
        {
            StorageType = storageType;
            FileName = filename;
        }

        public SettingsStorageAttribute(Type customStorageType)
        {
            CustomStorageType = customStorageType;
        }

        internal static ISettingsStorage ResolveStorage(Type settingsType)
        {
            var attr = settingsType.GetCustomAttribute<SettingsStorageAttribute>();
            if (attr == null)
            {
                return new SettingsStorage.Prefs();
            }
            if (attr.StorageType != 0)
            {
                return attr.StorageType.Resolve(settingsType);
            }
            if (attr.CustomStorageType != null)
            {
                var storageType = attr.CustomStorageType;
                if (!typeof(ISettingsStorage).IsAssignableFrom(storageType))
                {
                    Debug.LogWarning($"Type {storageType} not implements {typeof(ISettingsStorage).FullName}");
                }
                else
                {
                    var typeConstructor = storageType.GetConstructor(new Type[] { typeof(Type) });
                    if (typeConstructor != null)
                    {
                        return (ISettingsStorage)typeConstructor.Invoke(new object[] { storageType });
                    }
                    var emptyConstructor = storageType.GetConstructor(new Type[] { });
                    if (emptyConstructor != null)
                    {
                        return (ISettingsStorage)emptyConstructor.Invoke(new object[] { });
                    }
                }
                Debug.LogWarning($"No suitable constr in {storageType.Name} expected {storageType.Name}() or {storageType.Name}(Type settingsType)");
            }
            Debug.LogWarning($"Attribute [SettingsStorage] exists for {settingsType.FullName} but default settings {typeof(SettingsStorage.Prefs).Name} created");
            return new SettingsStorage.Prefs();
        }

        internal static string ResolveFileName(Type settingsType)
        {
            var attr = settingsType.GetCustomAttribute<SettingsStorageAttribute>();
            if (attr == null || string.IsNullOrWhiteSpace(attr.FileName))
            {
                return DefaultFileName;
            }
            var name = attr.FileName.Trim();
            if (name != attr.FileName)
            {
                Debug.LogWarning($"FileName trimmed \"{attr.FileName}\"");
            }
            try
            {
                if (string.Format(name, 0) == string.Format(name, 1))
                {
                    Debug.LogWarning($"Filename \"{name}\" must contains \"{{0}}\" substring for different users");
                    return DefaultFileName;
                }
            } catch (Exception exc)
            {
                Debug.LogWarning($"Error when format filename \"{name}\"");
                Debug.LogException(exc);
                return DefaultFileName;
            }
            return name;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SettingsHashAttribute : Attribute
    {
        public readonly HashType HashType = 0;
        public readonly Type CustomHashType;
        public readonly string Salt;

        public SettingsHashAttribute(HashType type, string salt)
        {
            HashType = type;
            Salt = salt;
        }

        public SettingsHashAttribute(Type customType, string salt)
        {
            CustomHashType = customType;
            Salt = salt;
        }

        internal static ISettingsHash Resolve(Type settingsType, out string salt)
        {
            var attr = settingsType.GetCustomAttribute<SettingsHashAttribute>();
            if (attr == null)
            {
                salt = null;
                return null;
            }
            salt = attr.Salt;
            if (string.IsNullOrEmpty(salt))
            {
                Debug.LogWarning($"No salt for {settingsType.Name} hash function of {settingsType.FullName}");
            }
            if (attr.HashType != 0)
            {
                return attr.HashType.Resolve();
            }
            if (attr.CustomHashType != null)
            {
                var hashType = attr.CustomHashType;
                if (typeof(ISettingsHash).IsAssignableFrom(hashType))
                {
                    var emptyConstructor = hashType.GetConstructor(new Type[] { });
                    if (emptyConstructor != null)
                    {
                        return (ISettingsHash)emptyConstructor.Invoke(new object[] { });
                    }
                    Debug.LogWarning($"No default constructor for type {hashType.FullName}");
                } else if (typeof(HashAlgorithm).IsAssignableFrom(hashType))
                {
                    var emptyConstructor = hashType.GetConstructor(new Type[] { });
                    if (emptyConstructor != null)
                    {
                        var algorithm = (HashAlgorithm)emptyConstructor.Invoke(new object[] { });
                        return new SettingsStorage.SettingsHash(algorithm);
                    }
                    Debug.LogWarning($"No default constructor for type {hashType.FullName}");
                } else
                {
                    Debug.LogWarning($"Type {hashType.FullName} not implements {typeof(ISettingsHash).FullName} or {typeof(HashAlgorithm).FullName}");
                }
            }
            Debug.LogWarning($"Attribute [SettingsUserIdHash] exists for {settingsType.FullName} but hash not found");
            return null;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SettingsEnabledAttribute : SettingsPredicateAttribute
    {
        public SettingsEnabledAttribute(string key, Op op, params string[] values) : base(key, op, values)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
    public sealed class SettingsVisibleAttribute : SettingsPredicateAttribute
    {
        public SettingsVisibleAttribute(string key, Op op, params string[] values) : base(key, op, values)
        {
        }

        public SettingsVisibleAttribute(string group, string key, Op op, params string[] values) : base(group, key, op, values)
        {
        }
    }
}