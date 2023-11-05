using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace Fury.Settings
{
    internal class BindingButtonFactory : ISettingsKeyFactory
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
            if (keyType != typeof(BindingButton))
            {
                return null;
            }
            if (context.CurrentField == null || !context.CurrentField.IsStatic)
            {
                Debug.Log($"{nameof(BindingButtonKey)} request static FieldInfo");
                return null;
            }
            return new BindingButtonKey(context, group, keyName, attributes, getter, setter);
        }
    }

    public sealed partial class BindingButtonKey : SettingsKey<BindingButton>
    {
        public readonly BindingFilterFlags FilterFlags;

        internal BindingButtonKey(
            KeyContext context,
            SettingsGroup group,
            string keyName,
            IReadOnlyList<Attribute> attributes,
            Func<object> getter,
            Action<object> setter
            ) : base(group, keyName, typeof(BindingButton), attributes, getter, setter)
        {
            FilterFlags = BindingFilterAttribute.Resolve(attributes, context.CurrentField);

            if (group.Page.PrimaryGameObject != null)
            {
                var mediator = context.PrimaryRegistry.GetOrCreate(
                    null,
                    () => group.Page.PrimaryGameObject.AddComponent<BindingMediator>());
                mediator.ListenKey(context.CurrentField);
            }
        }

        protected override BindingButton ReadValue(object value)
        {
            var def = (BindingButton)SettingsController.DefaultKeys.Read(this);
            var curr = (BindingButton)value;

            if (curr._triggers == null || curr._triggers.Length < def._triggers.Length)
            {
                Array.Resize(ref curr._triggers, def._triggers.Length);
            }

            return curr;
        }

        protected override bool ValidateValue(ref BindingButton value)
        {
            return true;
        }

        protected override BindingButton ValueFromJson(JsonTextReader reader)
        {
            var s = new JsonSerializer();
            var dto = s.Deserialize<BindingButtonDTO>(reader);
            return dto.ToBinding();
        }

        protected override void ValueToJson(JsonTextWriter writer, BindingButton value)
        {
            var s = new JsonSerializer();
            s.DefaultValueHandling = DefaultValueHandling.Ignore;
            s.Serialize(writer, value.ToDTO());
        }

        protected override object WriteValue(BindingButton value)
        {
            return value;
        }
    }
}