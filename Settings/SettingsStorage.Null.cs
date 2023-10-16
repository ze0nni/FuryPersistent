using System.Collections.Generic;

namespace Fury.Settings
{
    internal static partial class SettingsStorage
    {
        internal sealed class Null : ISettingsStorage
        {
            public void Load(string userId, IReadOnlyDictionary<string, SettingsKey> keys)
            {
                
            }

            public void Save(string userId, IReadOnlyList<SettingsKey> keys)
            {
                
            }
        }
    }
}