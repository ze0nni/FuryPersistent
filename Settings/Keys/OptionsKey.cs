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
                var options = new List<OptionsKey.Option>();
                var indexCounter = 0;
                foreach (var field in keyField.FieldType.GetFields())
                {
                    if (field.FieldType == keyField.FieldType)
                    {
                        options.Add(
                            new OptionsKey.Option(
                                indexCounter++,
                                field.Name));
                    }
                }
                return new OptionsKey(group, keyField, options);
            }
            return null;
        }
    }

    public class OptionsKey : SettingsKey<string>
    {
        public int ValueIndex
        {
            get
            {
                for (var i = 0; i < Options.Count; i++)
                {
                    if (Options[i].Value == Value)
                    {
                        return i;
                    }
                }
                return -1;
            }
            set
            {
                Value = Options[value].Value;
            }
        }

        public class Option
        {
            public readonly string Title;
            public readonly string Value;
            internal Option(int index, string value)
            {
                Title = value;
                Value = value;
            }
        }

        public readonly IReadOnlyList<Option> Options;

        public OptionsKey(
            SettingsGroup group, FieldInfo keyField, IReadOnlyList<Option> options)
            : base(group, keyField)
        {
            Options = options;
        }

        protected override string ReadValue(object value)
        {
            var name = value == null ? null : value.ToString();
            foreach (var o in Options)
            {
                if (o.Value == name)
                {
                    return name;
                }
            }
            return Options.Count == 0 ? string.Empty : (string)SettingsController.DefaultKeys.Read(this);
        }

        protected override bool ValidateValue(ref string value)
        {
            foreach (var o in Options)
            {
                if (o.Value == value)
                {
                    return true;
                }
            }
            value = Options[0].Value;
            return true;
        }

        protected override object WriteValue(string value)
        {
            if (KeyField.FieldType.IsEnum)
            {
                if (Enum.TryParse(KeyType, value, out var e)) {
                    return e;
                } else
                {
                    return SettingsController.DefaultKeys.Read(this);
                }
            } else
            {
                return value;
            }
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