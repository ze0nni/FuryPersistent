using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Fury.Settings
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ScreenFullscreenAttribute : Attribute
    {

    }

    internal class FullScreenKeyFactory : ISettingsKeyFactory
    {
        public SettingsKey Produce(KeyContext context, SettingsGroup group, FieldInfo keyField)
        {
            if (keyField.FieldType == typeof(bool) 
                && keyField.GetCustomAttribute<ScreenFullscreenAttribute>() != null)
            {
                return new FullScreenKey(group, keyField);
            }
            return null;
        }
    }

    public class FullScreenKey : ToggleKey
    {
        public FullScreenKey(SettingsGroup group, FieldInfo keyField) : base(group, keyField, null)
        {
        }

        protected override void OnApply()
        {
            Screen.fullScreen = Value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ScreenResolutionAttribute : Attribute
    {

    }

    internal class ScreenResolutionKeyFactory : ISettingsKeyFactory
    {
        public SettingsKey Produce(KeyContext context, SettingsGroup group, FieldInfo keyField)
        {
            if (keyField.FieldType == typeof(string) && keyField.GetCustomAttribute<ScreenResolutionAttribute>() != null)
            {
                return new ScreenResolutionKey(group, keyField, Screen.resolutions.Select(r =>
                {
                    return new OptionsKey.Option($"{r.width}x{r.height}");
                }).ToArray());
            }
            return null;
        }
    }

    public class ScreenResolutionKey : OptionsKey
    {
        public ScreenResolutionKey(SettingsGroup group, FieldInfo keyField, IReadOnlyList<Option> options) : base(group, keyField, options)
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