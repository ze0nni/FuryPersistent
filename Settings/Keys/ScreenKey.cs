using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fury.Settings
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ScreenFullScreenAttribute : Attribute
    {

    }

    internal class FullScreenKeyFactory : ISettingsKeyFactory
    {
        public SettingsKey Produce(
            KeyContext context,
            SettingsGroup group,
            string keyName,
            Type keyType,
            IReadOnlyList<Attribute> attributes,
            Func<object> getter,
            Action<object> setter)
        {
            if (keyType == typeof(bool) 
                && attributes.Any(a => a is ScreenFullScreenAttribute))
            {
                return new FullScreenKey(group, keyName, attributes, getter, setter);
            }
            return null;
        }
    }

    public class FullScreenKey : ToggleKey
    {
        public FullScreenKey(
            SettingsGroup group,
            string keyName,
            IReadOnlyList<Attribute> attributes,
            Func<object> getter,
            Action<object> setter) : base(group, null, keyName, attributes, getter, setter)
        {
        }

        protected override void OnApply()
        {
            var r = Screen.currentResolution;
            var mode = Value ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
            Screen.fullScreen = Value;
            Screen.SetResolution(r.width, r.height, mode);
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ScreenResolutionAttribute : Attribute
    {

    }

    internal class ScreenResolutionKeyFactory : ISettingsKeyFactory
    {
        public SettingsKey Produce(
            KeyContext context,
            SettingsGroup group,
            string keyName,
            Type keyType,
            IReadOnlyList<Attribute> attributes,
            Func<object> getter,
            Action<object> setter)
        {
            if (keyType == typeof(string) && attributes.Any(a => a is ScreenResolutionAttribute))
            {
                return new ScreenResolutionKey(group, keyName, attributes, getter, setter, Screen.resolutions.Select(r =>
                {
                    return new OptionsKey.Option($"{r.width}x{r.height}", new Attribute[0]);
                })
                .Distinct()
                .ToArray());
            }
            return null;
        }
    }

    public class ScreenResolutionKey : OptionsKey
    {
        public ScreenResolutionKey(
            SettingsGroup group, 
            string keyName,
            IReadOnlyList<Attribute> attributes,
            Func<object> getter,
            Action<object> setter,
            IReadOnlyList<Option> options) : base(group, keyName, typeof(string), attributes, getter, setter, options)
        {
        }

        protected override void OnApply()
        {
            var parts = Value.Split("x");
            if (parts.Length != 2)
            {
                return;
            }
            if (!int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height))
            {
                return;
            }
            Screen.SetResolution(width, height, Screen.fullScreen);
        }

        public static string Default()
        {
            return $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
        }
    }
}