using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fury.Settings
{
    public interface ISettingsKeysSource
    {
        void Generate(FieldInfo field, SettingsKeyBuilder builder);
    }

    public sealed class SettingsKeyBuilder : IDisposable
    {
        SettingsGroup _group;
        KeyContext _context;
        List<SettingsKey> _keys;

        internal SettingsKeyBuilder(SettingsGroup group, KeyContext context, List<SettingsKey> keys)
        {
            _group = group;
            _context = context;
            _keys = keys;
        }

        static readonly Attribute[] EmptyAttributes = new Attribute[0];

        public void Add<T>(
            string keyName,
            Func<T> getter,
            Action<T> setter,
            params Attribute[] attributes)
        {
            var key = _group.Page.Controller.CreateKey(
                _context,
                _group,
                keyName,
                typeof(T),
                attributes,
                () => getter.Invoke(),
                v => setter.Invoke((T)v),
                out var headerKey);

            if (key == null)
            {
                return;
            }
            if (headerKey != null)
            {
                _keys.Add(headerKey);
            }
            _keys.Add(key);
        }

        public void Dispose()
        {
            _group = null;
            _context = null;
            _keys = null;
        }
    }
}