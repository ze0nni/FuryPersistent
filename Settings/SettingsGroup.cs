using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Fury.Settings.SettingsKey;

namespace Fury.Settings
{
    public sealed class SettingsGroup
    {
        public readonly string Name;
        public readonly SettingsPage Page;
        public readonly Type GroupType;
        public readonly Registry Registry = new Registry();

        bool _visible = true;
        public bool Visible => _visible;

        internal readonly DisplayPredecateDelegate _visiblePredicate;

        private bool _isDirtyKeys;
        internal void MarkKeysDirty() => _isDirtyKeys = true;
        private SettingsKey[] _keys;
        public IReadOnlyList<SettingsKey> Keys
        {
            get
            {
                if (_isDirtyKeys)
                {
                    _isDirtyKeys = false;
                    foreach (var k in _keys)
                    {
                        k.UpdateDisplayState(this);
                    }
                }
                return _keys;
            }
        }

        internal readonly Dictionary<string, SettingsKey> _keysMap = new Dictionary<string, SettingsKey>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SettingsKey GetKey(string keyNameOrKeyId)
        {
            if (_keysMap.TryGetValue(keyNameOrKeyId, out var value))
            {
                return value;
            }
            return Page.GetKey(keyNameOrKeyId);
        }

        public T GetKey<T>(string keyNameOrKeyId) 
            where T: SettingsKey
        {
            return (T)GetKey(keyNameOrKeyId);
        }

        public bool IsChanged { get; private set; }
        public event Action OnChanged;
        public event Action<SettingsKey> OnKeyChanged;

        internal SettingsGroup(SettingsPage page, Type groupType)
        {
            Name = groupType.Name;
            Page = page;
            GroupType = groupType;
            _visiblePredicate = SettingsPredicateAttribute.Resolve<SettingsVisibleAttribute>(
                groupType.GetCustomAttributes(true).Cast<Attribute>().ToArray());
        }

        internal void Setup()
        {
            var context = new KeyContext(Page.Controller.Registry, Page.Registrty, Registry);
            var keys = new List<SettingsKey>();
            foreach (var field in GroupType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (typeof(ISettingsKeysSource).IsAssignableFrom(field.FieldType))
                {
                    var source = (ISettingsKeysSource)Activator.CreateInstance(field.FieldType);
                    using (var builder = new SettingsKeyBuilder(this, context, keys))
                    {
                        source.Generate(field, builder);
                    }
                }
                else
                {
                    context.CurrentField = field;
                    var key = Page.Controller.CreateKey(context, this, field, out var headerKey);
                    context.CurrentField = null;
                    if (key == null)
                    {
                        continue;
                    }
                    if (headerKey != null)
                    {
                        keys.Add(headerKey);
                    }
                    keys.Add(key);
                }
            }
            foreach (var key in keys)
            {
                key.Setup();
                if (key.Type == KeyType.Key)
                {
                    _keysMap.Add(key.KeyName, key);
                }
            }
            _keys = keys.ToArray();
            SetKeys(keys.Cast<SettingsKey>());
        }

        protected void SetKeys(IEnumerable<SettingsKey> keys)
        {
            _keys = keys.ToArray();
        }

        internal void NotifyKeyChanged(SettingsKey key)
        {
            IsChanged = true;
            foreach (var k in _keys)
            {
                k.UpdateDisplayState(this);
            }
            OnKeyChanged?.Invoke(key);
            Page.NotifyKeyChanged(key);
        }

        internal void UpdateDisplayState()
        {
            var visible = _visiblePredicate == null ? true : _visiblePredicate(this);
            if (_visible == visible)
            {
                return;
            }
            _visible = visible;
            OnChanged?.Invoke();
        }

        internal void NotifySave()
        {
            IsChanged = false;
            foreach (var key in Keys)
            {
                key.NotifySave();
            }
        }

        internal void LoadDefault()
        {
            foreach (var key in Keys)
            {
                key.LoadDefault();
            }
        }

        internal void Apply()
        {
            foreach (var key in Keys)
            {
                key.Apply();
            }
            IsChanged = false;
        }

        internal void Reset()
        {
            foreach (var key in Keys)
            {
                key.Reset();
            }
            foreach (var key in Keys)
            {
                key.UpdateDisplayState(this);
            }
            IsChanged = false;
        }
    }
}
