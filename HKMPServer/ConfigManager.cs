using System.IO;
using Hkmp.Game.Settings;
using Hkmp.Util;

namespace HkmpServer {
    /// <summary>
    /// Config manager for managing server settings for the console program.
    /// </summary>
    internal static class ConfigManager {
        /// <summary>
        /// The file name of the server settings file.
        /// </summary>
        private const string ServerSettingsFileName = "serversettings.json";

        /// <summary>
        /// The file name of the console settings file.
        /// </summary>
        private const string ConsoleSettingsFileName = "consolesettings.json";

        /// <summary>
        /// Try to load stored <see cref="T"/> in the file with the given name. If not such file can be found it will
        /// return a fresh instance.
        /// </summary>
        /// <param name="settings">An instance of <see cref="T"/>.</param>
        /// <param name="fileName">The name of the file that contains the settings.</param>
        /// <typeparam name="T">The type of the settings class.</typeparam>
        /// <returns>True if the file already exists, false if a new instance of <see cref="T"/> has been created.
        /// </returns>
        private static bool LoadSettings<T>(out T settings, string fileName) where T : new() {
            var serverSettingsFilePath = Path.Combine(FileUtil.GetCurrentPath(), fileName);
            if (File.Exists(serverSettingsFilePath)) {
                settings = FileUtil.LoadObjectFromJsonFile<T>(serverSettingsFilePath);
                return true;
            }

            settings = new T();
            return false;
        }

        /// <summary>
        /// Will save the given instance of <see cref="T"/> to file with the given name.
        /// </summary>
        /// <param name="serverSettings">The instance of <see cref="T"/> that should be saved to file.</param>
        /// <param name="fileName">The name of the file where the settings should be saved.</param>
        /// <typeparam name="T">The type of the settings class.</typeparam>
        private static void SaveSettings<T>(T serverSettings, string fileName) {
            var serverSettingsFilePath = Path.Combine(FileUtil.GetCurrentPath(), fileName);

            FileUtil.WriteObjectToJsonFile(serverSettings, serverSettingsFilePath);
        }

        /// <summary>
        /// Load the server settings from the default location.
        /// </summary>
        /// <param name="serverSettings">An instance of <see cref="ServerSettings"/>.</param>
        /// <returns>True if the settings existed, false otherwise.</returns>
        public static bool LoadServerSettings(out ServerSettings serverSettings) =>
            LoadSettings(out serverSettings, ServerSettingsFileName);

        /// <summary>
        /// Save the server settings to the default location.
        /// </summary>
        /// <param name="serverSettings">The <see cref="ServerSettings"/> to save.</param>
        public static void SaveServerSettings(ServerSettings serverSettings) =>
            SaveSettings(serverSettings, ServerSettingsFileName);
        
        /// <summary>
        /// Load the console settings from the default location.
        /// </summary>
        /// <param name="consoleSettings">An instance of <see cref="ConsoleSettings"/>.</param>
        /// <returns>True if the settings existed, false otherwise.</returns>
        public static bool LoadConsoleSettings(out ConsoleSettings consoleSettings) =>
            LoadSettings(out consoleSettings, ConsoleSettingsFileName);

        /// <summary>
        /// Save the console settings to the default location.
        /// </summary>
        /// <param name="consoleSettings">The <see cref="ConsoleSettings"/> to save.</param>
        public static void SaveConsoleSettings(ConsoleSettings consoleSettings) =>
            SaveSettings(consoleSettings, ConsoleSettingsFileName);
    }
}
