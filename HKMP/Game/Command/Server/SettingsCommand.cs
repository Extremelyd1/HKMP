using System;
using System.Reflection;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;

namespace Hkmp.Game.Command.Server {
    public class SettingsCommand : IServerCommand {
        public string Trigger => "/set";
        public string[] Aliases => Array.Empty<string>();
        public bool AuthorizedOnly => true;

        private readonly ServerManager _serverManager;
        private readonly Settings.GameSettings _gameSettings;

        public SettingsCommand(ServerManager serverManager, Settings.GameSettings gameSettings) {
            _serverManager = serverManager;
            _gameSettings = gameSettings;
        }

        public void Execute(ICommandSender commandSender, string[] args) {
            if (args.Length < 2) {
                commandSender.SendMessage($"Usage: {Trigger} <name> [value]");
                return;
            }

            var settingName = args[1];

            var propertyInfos = typeof(Settings.GameSettings).GetProperties();

            PropertyInfo settingProperty = null;
            foreach (var prop in propertyInfos) {
                if (prop.Name.Equals(settingName)) {
                    settingProperty = prop;
                }
            }

            if (settingProperty == null || !settingProperty.CanRead) {
                commandSender.SendMessage($"Could not find setting with name: {settingName}");
                return;
            }

            if (args.Length < 3) {
                // The user only supplied the name of the setting, so we print its value
                var currentValue = settingProperty.GetValue(_gameSettings, null);
                
                commandSender.SendMessage($"Setting '{settingName}' currently has value: {currentValue}");
                return;
            }

            var newValueString = args[2];

            if (!settingProperty.CanWrite) {
                commandSender.SendMessage($"Could not change value of setting with name: {settingName} (non-writable)");
                return;
            }

            object newValueObject;

            if (settingProperty.PropertyType == typeof(bool)) {
                if (!bool.TryParse(newValueString, out var newValueBool)) {
                    commandSender.SendMessage("Please provide a boolean value (true/false) for this setting");
                    return;
                }

                newValueObject = newValueBool;
            } else if (settingProperty.PropertyType == typeof(byte)) {
                if (!byte.TryParse(newValueString, out var newValueByte)) {
                    commandSender.SendMessage("Please provide a byte value (>= 0 and <= 255) for this setting");
                    return;
                }

                newValueObject = newValueByte;
            } else {
                commandSender.SendMessage($"Could not change value of setting with name: {settingName} (unhandled type)");
                return;
            }
            
            settingProperty.SetValue(_gameSettings, newValueObject, null);
                
            commandSender.SendMessage($"Changed setting '{settingName}' to: {newValueObject}");
            
            _serverManager.OnUpdateGameSettings();
        }
    }
}