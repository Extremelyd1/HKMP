using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using HKMP.Game.Settings;

namespace HKMPServer {
    public static class ConfigManager {
        private const string GameSettingsFileName = "gamesettings.json";

        /**
         * Try to load stored game settings in the default location.
         * If not such file can be found it will return a fresh instance.
         * Will write the parameter 'existed' to indicate whether the file already existed or if a
         * new instance has been generated.
         */
        public static GameSettings LoadGameSettings(out bool existed) {
            var gameSettingsFilePath = Path.Combine(GetCurrentPath(), GameSettingsFileName);
            if (File.Exists(gameSettingsFilePath)) {
                var deserializer = new DataContractJsonSerializer(typeof(GameSettings));

                var readStream = File.OpenRead(gameSettingsFilePath);

                try {
                    var gameSettings = (GameSettings) deserializer.ReadObject(readStream);

                    readStream.Close();

                    existed = true;
                    return gameSettings;
                } catch (SerializationException) {
                    readStream.Close();
                    
                    Console.WriteLine($"Could not read {GameSettingsFileName}, it was invalid. Replacing it by fresh instance");
                }
            }

            existed = false;
            return new GameSettings();
        }

        /**
         * Will save the given instance of GameSettings to the default file location.
         */
        public static void SaveGameSettings(GameSettings gameSettings) {
            var serializer = new DataContractJsonSerializer(typeof(GameSettings));

            var gameSettingsFilePath = Path.Combine(GetCurrentPath(), GameSettingsFileName);

            var writeStream = File.OpenWrite(gameSettingsFilePath);

            serializer.WriteObject(writeStream, gameSettings);
            
            writeStream.Close();
        }

        /**
         * Get the path of where the program is executing.
         */
        private static string GetCurrentPath() {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) {
                throw new Exception("Could not get entry assembly");
            }

            var currentPath = Path.GetDirectoryName(entryAssembly.Location);
            if (currentPath == null) {
                throw new Exception("Could not get directory of entry assembly");
            }

            return currentPath;
        }
    }
}