using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Hkmp.Util {
    public static class FileUtil {
        private const string LogObjectName = "Hkmp.Util.FileUtil";

        public static T LoadObjectFromJsonFile<T>(string filePath) {
            try {
                var fileContents = File.ReadAllText(filePath);

                return JsonConvert.DeserializeObject<T>(fileContents);
            } catch (Exception e) {
                Logger.Get().Warn(LogObjectName,
                    $"Could not read file at path: {filePath}, exception: {e.GetType()}, {e.Message}");
                return default;
            }
        }

        public static void WriteObjectToJsonFile<T>(T obj, string filePath) {
            try {
                var serializedObj = JsonConvert.SerializeObject(obj, Formatting.Indented);

                File.WriteAllText(filePath, serializedObj);
            } catch (Exception e) {
                Logger.Get().Warn(LogObjectName,
                    $"Could not write file at path: {filePath}, exception: {e.GetType()}, {e.Message}");
            }
        }
        
        /// <summary>
        /// Get the path of where the program is executing.
        /// </summary>
        /// <returns>The current path of the executing assembly.</returns>
        /// <exception cref="Exception">Thrown if the assembly can not be found or the directory of the assembly
        /// can not be found.</exception>
        public static string GetCurrentPath() {
            Assembly assembly = null;

            try {
                assembly = Assembly.GetEntryAssembly();
            } catch {
                // ignored
            }

            if (assembly == null) {
                try {
                    assembly = Assembly.GetExecutingAssembly();
                } catch {
                    throw new Exception("Could not get assembly for file path");
                }
            }

            var currentPath = Path.GetDirectoryName(assembly.Location);
            if (currentPath == null) {
                throw new Exception("Could not get directory of entry assembly");
            }

            return currentPath;
        }
    }
}