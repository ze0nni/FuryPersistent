using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fury.Settings.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Audio;

namespace Fury.Settings
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class AudioLevelAttribute : Attribute
    {
        public readonly string Mixer;
        public readonly string Parameter;
        public AudioLevelAttribute (string mixer, string parameter)
        {
            Mixer = mixer;
            Parameter = parameter;
        }
    }

    internal class AudioLevelKeyFactory : ISettingsKeyFactory
    {
        public SettingsKey Produce(
            KeyContext context,
            SettingsGroup group,
            FieldInfo keyField)
        {
            if (keyField.FieldType == typeof(float))
            {
                var attrs = keyField.GetCustomAttributes<AudioLevelAttribute>().ToArray();
                if (attrs.Length > 0)
                {
                    return new AudioLevelKey(group, keyField, attrs);
                }
            }
            return null;
        }
    }

    internal sealed class AudioLevelKey : SettingsKey<float>
    {
        readonly List<(AudioMixer Mixer, string Parameter)> _mixers = new List<(AudioMixer, string)>();

        public AudioLevelKey(SettingsGroup group, FieldInfo keyField, AudioLevelAttribute[] attrs) : base(group, keyField)
        {
            foreach (var a in attrs)
            {
                var mixer = Resources.Load<AudioMixer>(a.Mixer);
                if (mixer == null)
                {
                    Debug.LogWarning($"AudioMixer with name {a.Mixer} not found in Resources folder");
                    continue;
                }
                _mixers.Add((mixer, a.Parameter));
            }
        }

        protected override void OnApply()
        {
            var vol = (1 - Mathf.Sqrt(Value)) * -80f;
            foreach (var m in _mixers)
            {
                m.Mixer.SetFloat(m.Parameter, vol);
            }
        }

        protected override float ReadValue(object value)
        {
            switch (value)
            {
                case float f:
                    return f;
            }
            return 1;
        }

        protected override object WriteValue(float value)
        {
            return value;
        }

        protected override bool ValidateValue(ref float value)
        {
            value = Mathf.Clamp01(value);
            return true;
        }

        protected override float ValueFromJson(JsonTextReader reader)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                    if (float.TryParse(reader.Value.ToString(), out var n))
                        return n;
                    break;
                default:
                    reader.Skip();
                    break;
            }
            Debug.LogWarning($"Return default value for key {Id}");
            return (float)SettingsController.DefaultKeys.Read(this);
        }

        protected override void ValueToJson(JsonTextWriter writer, float value)
        {
            writer.WriteValue(value);
        }

        protected internal override void OnFieldGUI(ISettingsGUIState state, float containerWidth)
        {
            var newValue = GUILayout.HorizontalSlider(Value, 0, 1);
            if (newValue != Value)
            {
                Value = newValue;
            }
        }
    }
}