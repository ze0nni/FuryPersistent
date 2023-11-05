using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fury.Settings
{
    [Flags]
    public enum BindingFilterFlags
    {
        Keyboard = 1 << 0,
        Joystick = 1 << 1,
        MouseKeys = 1 << 2,
        MouseAxis = 1 << 3,
        All = Keyboard | Joystick | MouseKeys | MouseAxis
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class BindingFilterAttribute : Attribute
    {
        public BindingFilterFlags Flags;
        public BindingFilterAttribute(BindingFilterFlags flags) => Flags = flags;

        public static BindingFilterFlags Resolve(IReadOnlyList<Attribute> attributes, FieldInfo fieldInfo)
        {
            var resultFlags = BindingFilterFlags.All;

            var fieldAttr = attributes.Where(a => a is BindingFilterAttribute).Cast<BindingFilterAttribute>().FirstOrDefault();
            if (fieldAttr != null)
            {
                return fieldAttr.Flags;
            }

            void FindInParent(Type parent, ref BindingFilterFlags flags)
            {
                if (parent == null)
                {
                    return;
                }
                var attr = parent.GetCustomAttribute<BindingFilterAttribute>();
                if (attr != null)
                {
                    flags = attr.Flags;
                }
                else
                {
                    FindInParent(parent.DeclaringType, ref flags);
                }
            }
            if (fieldInfo != null)
            {
                FindInParent(fieldInfo.DeclaringType, ref resultFlags);
            }

            return resultFlags;
        }
    }
}