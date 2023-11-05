using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Fury.Settings
{
    internal class BindingAxisFactory : ISettingsKeyFactory
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
            if (keyType != typeof(BindingAxis))
            {
                return null;
            }
            return new BindingAxisKey(context, group, keyName, attributes, getter, setter);
        }
    }

    public sealed partial class BindingAxisKey : SettingsKey<BindingAxis>
    {
        public readonly BindingFilterFlags FilterFlags;

        public BindingAxisKey(
            KeyContext context,
            SettingsGroup group,
            string keyName,
            IReadOnlyList<Attribute> attributes,
            Func<object> getter,
            Action<object> setter
            ) : base(group, keyName, typeof(BindingAxis), attributes, getter, setter)
        {
            FilterFlags = BindingFilterAttribute.Resolve(attributes, context.CurrentField);

            if (group.Page.PrimaryGameObject != null)
            {
                var mediator = context.PrimaryRegistry.GetOrCreate(
                    null,
                    () => group.Page.PrimaryGameObject.AddComponent<BindingMediator>());
                mediator.ListenAxis(context.CurrentField);
            }
        }

        protected override BindingAxis ReadValue(object value)
        {
            var def = (BindingAxis)SettingsController.DefaultKeys.Read(this);
            var curr = (BindingAxis)value;

            if (curr._triggers == null || curr._triggers.Length < def._triggers.Length)
            {
                Array.Resize(ref curr._triggers, def._triggers.Length);
            }

            return curr;
        }

        protected override bool ValidateValue(ref BindingAxis value)
        {
            return true;
        }

        protected override BindingAxis ValueFromJson(JsonTextReader reader)
        {
            var s = new JsonSerializer();
            var dto = s.Deserialize<BindingAxisDTO>(reader);
            return dto.ToBinding();
        }

        protected override void ValueToJson(JsonTextWriter writer, BindingAxis value)
        {
            var s = new JsonSerializer();
            s.DefaultValueHandling = DefaultValueHandling.Ignore;
            s.Serialize(writer, value.ToDTO());
        }

        protected override object WriteValue(BindingAxis value)
        {
            return value;
        }
    }
}