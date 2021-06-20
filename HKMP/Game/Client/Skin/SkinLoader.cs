using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Hkmp.Game.Client.Skin {
    /**
     * Class responsible for disk interaction for all skin related operations.
     */
    public class SkinLoader {
        private const string ModFolderName = "HKMP";
        private const string SkinFolderName = "Skins";

        // The names of the texture files
        private const string KnightTextureFileName = "Knight.png";
        private const string SprintTextureFileName = "Sprint.png";

        // The name of the file that contains the ID
        private const string IdFileName = "id.txt";

        private readonly string _skinFolderPath;

        public SkinLoader() {
            var modsFolderPath = GetModsFolder();

            _skinFolderPath = CombinePaths(modsFolderPath, ModFolderName, SkinFolderName);
            if (!Directory.Exists(_skinFolderPath)) {
                Directory.CreateDirectory(_skinFolderPath);
            }
        }

        /**
         * Load all skins on disk in the given path into the given Dictionary.
         * Assumes that the given Dictionary is non-null.
         */
        public void LoadAllSkins(ref Dictionary<byte, PlayerSkin> skins) {
            if (!Directory.Exists(_skinFolderPath)) {
                Logger.Get().Warn(this, $"Tried to load all skins, but directory: {_skinFolderPath} did not exist");
                return;
            }

            var directoryPaths = Directory.GetDirectories(_skinFolderPath);
            if (directoryPaths.Length == 0) {
                Logger.Get().Info(this, $"No skins can be loaded since there are no directories in: {_skinFolderPath}");
                return;
            }

            // Mapping of directory paths that do not have a file containing a valid ID to their skin
            var directoriesWithoutId = new Dictionary<string, PlayerSkin>();
            // Set of valid IDs that have been used for skins already
            var idsUsed = new HashSet<byte>();

            // We first loop over all directories and check whether they contain a file indicating their ID
            foreach (var directoryPath in directoryPaths) {
                // Try to load the player skin in this directory
                if (!LoadTexturesForSkin(directoryPath, out var playerSkin)) {
                    Logger.Get().Warn(this, $"Tried to load player skin in directory: {directoryPath}, but failed");
                    continue;
                }

                // Check whether an ID file exists
                var idFilePath = Path.Combine(directoryPath, IdFileName);
                if (!File.Exists(idFilePath)) {
                    directoriesWithoutId[directoryPath] = playerSkin;
                    continue;
                }

                // Read the ID from the file and do sanity checks an whether it is a valid ID
                var id = ReadIntFromFile(idFilePath);
                if (id == -1) {
                    Logger.Get().Warn(this, $"Tried to load player skin, but ID: {id} is not valid");
                    directoriesWithoutId[directoryPath] = playerSkin;
                    continue;
                }

                if (id > 255 || id < 1) {
                    Logger.Get().Warn(this, $"Tried to load player skin, but ID: {id} is not valid (< 1 or > 255)");
                    directoriesWithoutId[directoryPath] = playerSkin;
                    continue;
                }

                var idByte = (byte) id;

                Logger.Get().Info(this, $"Successfully loaded skin in directory: {directoryPath}, given ID: {idByte}");

                // Save it in the mapping and overwrite an existing entry
                skins[idByte] = playerSkin;
                // Also save the ID in a set so we know it is used already
                idsUsed.Add(idByte);
            }

            // Now we loop over the directories that didn't have an ID yet
            foreach (var directorySkinPair in directoriesWithoutId) {
                var directoryPath = directorySkinPair.Key;

                var idFilePath = Path.Combine(directoryPath, IdFileName);

                // Whether the file exists or not, this will give a StreamWriter that (over)writes the file
                var streamWriter = File.CreateText(idFilePath);

                // Find the lowest byte that hasn't been used yet for an ID
                int id;
                for (id = 1; id < 256; id++) {
                    if (!idsUsed.Contains((byte) id)) {
                        break;
                    }
                }

                if (id > 255) {
                    Logger.Get().Warn(this,
                        "Could not find a valid ID for this skin, perhaps you have used all 255 slots?");
                    return;
                }

                var idByte = (byte) id;

                Logger.Get().Info(this, $"Successfully loaded skin in directory: {directoryPath}, given ID: {idByte}");

                // Write the ID to the file and close the StreamWriter
                streamWriter.Write(id);
                streamWriter.Close();

                // Save it in the mapping and overwrite an existing entry
                skins[idByte] = directorySkinPair.Value;
                // Also save the ID in a set so we know it is used now
                idsUsed.Add(idByte);
            }
        }

        /**
         * Try to load the textures for a player skin from disk at the given path.
         * This path should be the full path ending in the directory that contains the texture files.
         */
        private bool LoadTexturesForSkin(string path, out PlayerSkin playerSkin) {
            // Fallback out value to make sure we can always return false if loading failed
            playerSkin = new PlayerSkin(null, null);

            if (!Directory.Exists(path)) {
                return false;
            }

            var knightPath = Path.Combine(path, KnightTextureFileName);
            if (!LoadTexture(knightPath, out var knightTexture)) {
                return false;
            }

            var sprintPath = Path.Combine(path, SprintTextureFileName);
            if (!LoadTexture(sprintPath, out var sprintTexture)) {
                return false;
            }

            playerSkin = new PlayerSkin(knightTexture, sprintTexture);
            return true;
        }

        private bool LoadTexture(string filePath, out Texture2D texture) {
            texture = null;

            if (!File.Exists(filePath)) {
                Logger.Get().Info(this,
                    $"Tried to load texture at: {filePath}, but it didn't exist");
                return false;
            }

            var textureBytes = File.ReadAllBytes(filePath);
            texture = new Texture2D(1, 1);

            return texture.LoadImage(textureBytes, true);
        }

        /**
         * Reads an integer from a file at the given path.
         * Returns -1 if the file contents cannot be parsed to an int.
         */
        private static int ReadIntFromFile(string path) {
            var fileContent = File.ReadAllText(path);
            if (!int.TryParse(fileContent, out var id)) {
                return -1;
            }

            return id;
        }

        private static string GetModsFolder() {
            switch (SystemInfo.operatingSystemFamily) {
                case OperatingSystemFamily.MacOSX:
                    return Path.GetFullPath($"{Application.dataPath}/Resources/Data/Managed/Mods");
                default:
                    return Path.GetFullPath($"{Application.dataPath}/Managed/Mods");
            }
        }

        /**
         * Combines the variable number of given existing paths into a complete path.
         * Uses Path.Combine for intermediate steps.
         */
        private static string CombinePaths(params string[] paths) {
            if (paths.Length == 0) {
                return "";
            }

            if (paths.Length == 1) {
                return paths[0];
            }

            // A StringBuilder would be more efficient if the size of the input was significantly large.
            // But we only call it with at most 5 paths or so, so it doesn't matter.
            var resultPath = "";
            foreach (var path in paths) {
                resultPath = Path.Combine(resultPath, path);
            }

            return resultPath;
        }
    }
}