using System;

namespace HKMP.UI {
    public class SettingsEntry {
        public string Name { get; }
        
        public Type Type { get; }
        public object DefaultValue { get; }
        public Action<object> ApplySetting { get; }

        public SettingsEntry(string name, Type type, object defaultValue, Action<object> applySetting) {
            Name = name;
            Type = type;
            DefaultValue = defaultValue;
            ApplySetting = applySetting;
        }
    }
}