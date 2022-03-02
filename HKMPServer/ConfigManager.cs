using System.IO;
using Hkmp.Game.Settings;
using Hkmp.Util;

namespace HkmpServer {
    public static class ConfigManager {
        private const string GameSettingsFileName = "gamesettings.json";

        /// <summary>
        /// Try to load stored game settings in the default location. If not such file can be found it
        /// will return a fresh instance.
        /// </summary>
        /// <param name="existed">Will be set to true if the file already exists, false if a new instance
        /// has been generated.</param>
        /// <returns>An instance of game settings.</returns>
        public static GameSettings LoadGameSettings(out bool existed) {
            var gameSettingsFilePath = Path.Combine(FileUtil.GetCurrentPath(), GameSettingsFileName);
            if (File.Exists(gameSettingsFilePath)) {
                existed = true;
                return FileUtil.LoadObjectFromJsonFile<GameSettings>(gameSettingsFilePath);
            }

            existed = false;
            return new GameSettings();
        }

        /// <summary>
        /// Will save the given instance of GameSettings to the default file location.
        /// </summary>
        /// <param name="gameSettings"></param>
        public static void SaveGameSettings(GameSettings gameSettings) {
            var gameSettingsFilePath = Path.Combine(FileUtil.GetCurrentPath(), GameSettingsFileName);

            FileUtil.WriteObjectToJsonFile(gameSettings, gameSettingsFilePath);
        }
    }
}