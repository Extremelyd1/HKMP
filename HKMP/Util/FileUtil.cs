using System;
using System.IO;
using System.Reflection;
using Hkmp.Logging;
using Newtonsoft.Json;

namespace Hkmp.Util;

/// <summary>
/// Class for utilities regarding file interaction.
/// </summary>
internal static class FileUtil {
    /// <summary>
    /// Load an object from a JSON file at the given path.
    /// </summary>
    /// <param name="filePath">The path of the file.</param>
    /// <typeparam name="T">The type of the object to load.</typeparam>
    /// <returns>An instance of the loaded object, or the default value if it could not be loaded.</returns>
    public static T LoadObjectFromJsonFile<T>(string filePath) {
        try {
            var fileContents = File.ReadAllText(filePath);

            return JsonConvert.DeserializeObject<T>(fileContents);
        } catch (Exception e) {
            Logger.Warn($"Could not read file at path \"{filePath}\", exception:\n{e}");
            return default;
        }
    }

    /// <summary>
    /// Write an object to a JSON file at the given path.
    /// </summary>
    /// <param name="obj">The object the write.</param>
    /// <param name="filePath">The path of the file.</param>
    /// <typeparam name="T">The type of the object to write.</typeparam>
    public static void WriteObjectToJsonFile<T>(T obj, string filePath) {
        try {
            var serializedObj = JsonConvert.SerializeObject(obj, Formatting.Indented);

            File.WriteAllText(filePath, serializedObj);
        } catch (Exception e) {
            Logger.Warn($"Could not write file at path \"{filePath}\", exception:\n{e}");
        }
    }

    /// <summary>
    /// Load an object from a JSON file at the given resource path.
    /// </summary>
    /// <param name="resourcePath">The path of the embedded resource.</param>
    /// <typeparam name="T">The type of the object to load.</typeparam>
    /// <returns>An instance of the loaded object, or the default value if it could not be loaded.</returns>
    public static T LoadObjectFromResourcePath<T>(string resourcePath) {
        var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
        if (resourceStream == null) {
            Logger.Warn($"Could not read resource at path: {resourcePath}");
            return default;
        }

        using var streamReader = new StreamReader(resourceStream);
        var fileString = streamReader.ReadToEnd();

        return JsonConvert.DeserializeObject<T>(fileString);
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
