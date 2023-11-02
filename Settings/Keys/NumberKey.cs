using Fury.Settings.UI;
using Newtonsoft.Json;
using System;
using System.Reflection;
using UnityEngine;

namespace Fury.Settings
{
    internal class NumberKeyFactory : ISettingsKeyFactory
    {
        public SettingsKey Produce(
            KeyContext context,
            SettingsGroup group,
            FieldInfo keyField)
        {
            if (keyField.FieldType == typeof(int)
                || keyField.FieldType == typeof(float))
            {
                var range = keyField.GetCustomAttribute<RangeAttribute>();
                return new NumberKey(
                    group,
                    keyField,
                    keyField.FieldType == typeof(int) ? NumberKey.NumberType.Int : NumberKey.NumberType.Float,
                    range == null ? null : (range.min, range.max));
            }
            return null;
        }
    }

    public class NumberKey : SettingsKey<float>
    {
        public enum NumberType
        {
            Int,
            Float
        }

        public readonly NumberType NumType;
        public readonly (float Min, float Max)? Range;

        public NumberKey(
            SettingsGroup group,
            FieldInfo keyField,
            NumberType numberType,
            (float, float)? range
        ) : base(group, keyField)
        {
            NumType = numberType;
            Range = range;
        }

        protected override bool ValidateValue(ref float value)
        {
            if (Range != null)
            {
                value = Mathf.Clamp(value, Range.Value.Min, Range.Value.Max);
            }
            if (NumType == NumberType.Int)
            {
                value = (int)value;
            }
            return true;
        }

        protected override float ReadValue(object value)
        {
            switch (value)
            {
                case int i: return i;
                case float f: return f;
            }
            return 0;
        }

        protected override object WriteValue(float value)
        {
            switch (NumType)
            {
                case NumberType.Int:
                    return (int)value;
                case NumberType.Float:
                    return value;
                default:
                    throw new ArgumentNullException(NumType.ToString());
            }
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
            if (NumType == NumberType.Int)
            {
                writer.WriteValue((int)value);
            }
            else
            {
                writer.WriteValue(value);
            }
        }

        protected internal override void OnFieldGUI(ISettingsGUIState state, float containerWidth)
        {
            if (Range == null)
            {
                var newValue = GUILayout.TextField(StringValue);
                if (float.TryParse(newValue, out var n))
                {
                    if (n != Value)
                    {
                        Value = n;
                    }
                }
                else
                {
                    Value = 0;
                }
                if (GUILayout.Button("-", GUILayout.Width(42)))
                {
                    Value -= 1;
                }
                if (GUILayout.Button("+", GUILayout.Width(42)))
                {
                    Value += 1;
                }
            }
            else
            {
                GUILayout.Label(StringValue, GUILayout.Width(48));
                var newValue = GUILayout.HorizontalSlider(Value, Range.Value.Min, Range.Value.Max);
                if (newValue != Value)
                {
                    Value = newValue;
                }
            }
        }
    }
}