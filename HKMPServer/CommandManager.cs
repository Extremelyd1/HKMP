using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using HKMP.Game.Server;
using HKMP.Game.Settings;

namespace HKMPServer {
    public class CommandManager {
        private delegate void CommandHandler(string[] args);

        private readonly GameSettings _gameSettings;
        private readonly ServerManager _serverManager;

        private readonly Dictionary<string, CommandHandler> _commandHandlers;

        public CommandManager(GameSettings gameSettings, ServerManager serverManager) {
            _gameSettings = gameSettings;
            _serverManager = serverManager;

            _commandHandlers = new Dictionary<string, CommandHandler>();

            RegisterCommandHandler("exit", ExitHandler);

            RegisterCommandHandler("setting", SettingsHandler);
            RegisterCommandHandler("set", SettingsHandler);

            RegisterCommandHandler("list", ListHandler);

            StartReadLoop();
        }

        private void ListHandler(string[] args) {
            var playerNames = _serverManager.GetPlayerNames();

            Console.WriteLine($"Online players ({playerNames.Length}): {string.Join(", ", playerNames)}");
        }

        private void SettingsHandler(string[] args) {
            if (args.Length == 0) {
                Console.WriteLine("Usage: setting <name> [value]");
                return;
            }

            var settingName = args[0];

            var propertyInfos = typeof(GameSettings).GetProperties();

            PropertyInfo settingProperty = null;
            foreach (var prop in propertyInfos) {
                if (prop.Name.Equals(settingName)) {
                    settingProperty = prop;
                }
            }

            if (settingProperty == null || !settingProperty.CanRead) {
                Console.WriteLine($"Could not find setting with name: {settingName}");
                return;
            }

            if (args.Length == 1) {
                // The user only supplied the name of the setting, so we print its value
                var currentValue = settingProperty.GetValue(_gameSettings, null);
                
                Console.WriteLine($"Setting '{settingName}' currently has value: {currentValue}");
                return;
            }

            var newValueString = args[1];

            if (!settingProperty.CanWrite) {
                Console.WriteLine($"Could not change value of setting with name: {settingName} (non-writable)");
                return;
            }

            object newValueObject;

            if (settingProperty.PropertyType == typeof(bool)) {
                if (!bool.TryParse(newValueString, out var newValueBool)) {
                    Console.WriteLine("Please provide a boolean value (true/false) for this setting");
                    return;
                }

                newValueObject = newValueBool;
            } else if (settingProperty.PropertyType == typeof(byte)) {
                if (!byte.TryParse(newValueString, out var newValueByte)) {
                    Console.WriteLine("Please provide a byte value (>= 0 and <= 255) for this setting");
                    return;
                }

                newValueObject = newValueByte;
            } else {
                Console.WriteLine($"Could not change value of setting with name: {settingName} (unhandled type)");
                return;
            }
            
            settingProperty.SetValue(_gameSettings, newValueObject, null);
                
            Console.WriteLine($"Changed setting '{settingName}' to: {newValueObject}");
            
            _serverManager.OnUpdateGameSettings();
        }

        private void ExitHandler(string[] args) {
            _serverManager.Stop();
            
            Console.WriteLine("Exiting server...");
            Environment.Exit(0);
        }

        private void StartReadLoop() {
            new Thread(() => {
                while (true) {
                    var consoleInput = Console.ReadLine();
                    if (consoleInput == null) {
                        continue;
                    }

                    var splitInput = consoleInput.Split(' ');
                    if (splitInput.Length == 0) {
                        continue;
                    }

                    var commandName = splitInput[0];
                    if (!_commandHandlers.ContainsKey(commandName)) {
                        Console.WriteLine($"Unknown command: {commandName}");
                        continue;
                    }

                    var args = new string[splitInput.Length - 1];
                    for (var i = 0; i < splitInput.Length - 1; i++) {
                        args[i] = splitInput[i + 1];
                    }

                    _commandHandlers[commandName].Invoke(args);
                }
            }).Start();
        }

        private void RegisterCommandHandler(string commandName, CommandHandler handler) {
            if (_commandHandlers.ContainsKey(commandName)) {
                return;
            }

            _commandHandlers[commandName] = handler;
        }
    }
}