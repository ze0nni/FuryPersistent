using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;

namespace Fury.Settings
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class AudioVolumeAttribute : Attribute
    {
        public readonly string Mixer;
        public readonly string Parameter;
        public AudioVolumeAttribute (string mixer, string parameter)
        {
            Mixer = mixer;
            Parameter = parameter;
        }
    }

    internal class AudioVolumeKeyFactory : ISettingsKeyFactory
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
            if (keyType == typeof(float))
            {
                var attrs = attributes.Where(a => a is AudioVolumeAttribute).Cast<AudioVolumeAttribute>().ToArray();
                if (attrs.Length > 0)
                {
                    return new AudioVolumeKey(group, keyName, attributes, getter, setter, attrs);
                }
            }
            return null;
        }
    }

    internal sealed class AudioVolumeKey : NumberKey
    {
        readonly List<(AudioMixer Mixer, string Parameter)> _mixers = new List<(AudioMixer, string)>();

        public AudioVolumeKey(
            SettingsGroup group,
            string keyName,
            IReadOnlyList<Attribute> attributes,
            Func<object> getter,
            Action<object> setter,
            AudioVolumeAttribute[] attrs) 
            : base(group, keyName, typeof(float), attributes, getter, setter, NumberType.Float, (0, 1))
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
    }
}