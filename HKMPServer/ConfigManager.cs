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
        /// Try to load stored <see cref="ServerSettings"/> in the default location. If not such file can be found it
        /// will return a fresh instance.
        /// </summary>
        /// <param name="existed">Will be set to true if the file already exists, false if a new instance
        /// has been generated.</param>
        /// <returns>An instance of <see cref="ServerSettings"/>.</returns>
        public static ServerSettings LoadServerSettings(out bool existed) {
            var serverSettingsFilePath = Path.Combine(FileUtil.GetCurrentPath(), ServerSettingsFileName);
            if (File.Exists(serverSettingsFilePath)) {
                existed = true;
                return FileUtil.LoadObjectFromJsonFile<ServerSettings>(serverSettingsFilePath);
            }

            existed = false;
            return new ServerSettings();
        }

        /// <summary>
        /// Will save the given instance of <see cref="ServerSettings"/> to the default file location.
        /// </summary>
        /// <param name="serverSettings"></param>
        public static void SaveServerSettings(ServerSettings serverSettings) {
            var serverSettingsFilePath = Path.Combine(FileUtil.GetCurrentPath(), ServerSettingsFileName);

            FileUtil.WriteObjectToJsonFile(serverSettings, serverSettingsFilePath);
        }
    }
}
