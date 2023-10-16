using System;
using System.Reflection;
using Newtonsoft.Json;

namespace Fury.Settings
{
    internal class BindingButtonFactory : ISettingsKeyFactory
    {
        public SettingsKey Produce(
            KeyContext context,
            SettingsGroup group,
            FieldInfo keyField)
        {
            if (keyField.FieldType != typeof(BindingButton))
            {
                return null;
            }
            return new BindingButtonKey(context, group, keyField);
        }
    }

    public sealed partial class BindingButtonKey : SettingsKey<BindingButton>
    {
        public readonly BindingFilterFlags FilterFlags;

        internal BindingButtonKey(
            KeyContext context,
            SettingsGroup group,
            FieldInfo keyField
            ) : base(group, keyField)
        {
            FilterFlags = BindingFilterAttribute.Resolve(keyField);

            if (group.Page.PrimaryGameObject != null)
            {
                var mediator = context.PrimaryRegistry.GetOrCreate(
                    null,
                    () => group.Page.PrimaryGameObject.AddComponent<BindingMediator>());
                mediator.ListenKey(keyField);
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