using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Fury.Settings
{
    public sealed class HeaderKey : SettingsKey
    {
        public HeaderKey(SettingsGroup group, HeaderAttribute header, IReadOnlyList<Attribute> attribytes)
            : base(group, header, attribytes)
        {
        }

        protected override void ApplyValue()
        {

        }

        protected override void ResetValue()
        {

        }

        internal override void Load(JsonTextReader reader)
        {
            throw new System.NotImplementedException();
        }

        internal override void Save(JsonTextWriter writer)
        {
            throw new System.NotImplementedException();
        }

        internal override void LoadDefault()
        {

        }

        internal override string ToJsonString()
        {
            throw new System.NotImplementedException();
        }
    }
}