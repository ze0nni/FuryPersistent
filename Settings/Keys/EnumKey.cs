using Fury.Settings.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fury.Settings
{
    internal class EnumKeyFactory : ISettingsKeyFactory
    {
        public SettingsKey Produce(
            KeyContext context,
            SettingsGroup group,
            FieldInfo keyField)
        {
            if (keyField.FieldType.IsEnum)
            {
                return new EnumKey(group, keyField);
            }
            return null;
        }
    }

    public sealed class EnumKey : SettingsKey<string>
    {
        public int ValueIndex
        {
            get => Array.IndexOf(_names, Value);
            set
            {
                Value = _names[value];
            }
        }

        public class Option
        {
            public readonly string Title;
            public readonly int Index;
            public readonly string Value;
            internal Option(int index, string value)
            {
                Title = value;
                Index = index;
                Value = value;
            }
        }

        public readonly IReadOnlyList<Option> Options;
        private readonly string[] _names;

        public EnumKey(SettingsGroup group, FieldInfo keyField) : base(group, keyField)
        {
            _names = Enum.GetNames(KeyType);
            var options = new List<Option>();
            var indexCounter = 0;
            foreach (var field in KeyType.GetFields())
            {
                if (field.FieldType == KeyType)
                {
                    options.Add(
                        new Option(
                            indexCounter++,
                            field.Name));
                }
            }
            Options = options;
        }

        protected override string ReadValue(object value)
        {
            var name = value == null ? null : value.ToString();
            var index = Array.IndexOf(_names, name);
            if (index == -1)
            {
                return _names[0];
            }
            return _names[index];
        }

        protected override bool ValidateValue(ref string value)
        {
            return Array.IndexOf(_names, value) != -1;
        }

        protected override object WriteValue(string value)
        {
            var e = Enum.Parse(KeyType, value);
            return e;
        }

        protected override string ValueFromJson(JsonTextReader read)
        {
            switch (read.TokenType)
            {
                case JsonToken.Null:
                case JsonToken.Undefined:
                    return null;
                case JsonToken.Boolean:
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                    return read.Value.ToString();
                default:
                    read.Skip();
                    break;
            }
            Debug.Log(read.TokenType);
            Debug.LogWarning($"Return default value for key {Id}");
            return (string)SettingsController.DefaultKeys.Read(this);
        }

        protected override void ValueToJson(JsonTextWriter writer, string value)
        {
            writer.WriteValue(value);
        }

        protected internal override void OnFieldGUI(ISettingsGUIState state, float containerWidth)
        {
            if (GUILayout.Button(StringValue))
            {
                state.ShowDropdownWindow(GuiRects.field, () =>
                {
                    foreach (var o in Options)
                    {
                        if (GUILayout.Button(o.Title))
                        {
                            Value = o.Value;
                            GUI.changed = true;
                            state.CloseWindow();
                        }
                    }
                });
            }
        }
    }
    
}