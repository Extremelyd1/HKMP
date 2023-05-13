using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Skin;

/// <summary>
/// Class responsible for disk interaction for all skin related operations.
/// </summary>
internal class SkinLoader {
    /// <summary>
    /// The name of the mod folder within the Hollow Knight installation.
    /// </summary>
    private const string ModFolderName = "HKMP";

    /// <summary>
    /// The name of the skin folder in the HKMP mod folder.
    /// </summary>
    private const string SkinFolderName = "Skins";

    /// <summary>
    /// The name of the Knight texture file.
    /// </summary>
    private const string KnightTextureFileName = "Knight.png";

    /// <summary>
    /// The name of the Sprint texture file.
    /// </summary>
    private const string SprintTextureFileName = "Sprint.png";

    /// <summary>
    /// The name of the file that contains the ID for a skin.
    /// </summary>
    private const string IdFileName = "id.txt";

    /// <summary>
    /// The full path of the skin folder.
    /// </summary>
    private readonly string _skinFolderPath;

    public SkinLoader() {
        var modsFolderPath = GetModsFolder();

        _skinFolderPath = CombinePaths(modsFolderPath, ModFolderName, SkinFolderName);
        if (!Directory.Exists(_skinFolderPath)) {
            Directory.CreateDirectory(_skinFolderPath);
        }
    }

    /// <summary>
    /// Load all skins on disk in the given path into the given Dictionary. Assumes that the given
    /// dictionary is non-null.
    /// </summary>
    /// <param name="skins">A non-null dictionary that will contain the loaded skins.</param>
    public void LoadAllSkins(ref Dictionary<byte, PlayerSkin> skins) {
        if (!Directory.Exists(_skinFolderPath)) {
            Logger.Warn($"Tried to load all skins, but directory: {_skinFolderPath} did not exist");
            return;
        }

        var directoryPaths = Directory.GetDirectories(_skinFolderPath);
        if (directoryPaths.Length == 0) {
            Logger.Warn($"No skins can be loaded since there are no directories in: {_skinFolderPath}");
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
                Logger.Warn($"Tried to load player skin in directory: {directoryPath}, but failed");
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
                Logger.Warn($"Tried to load player skin, but ID: {id} is not valid");
                directoriesWithoutId[directoryPath] = playerSkin;
                continue;
            }

            if (id > 255 || id < 1) {
                Logger.Warn($"Tried to load player skin, but ID: {id} is not valid (< 1 or > 255)");
                directoriesWithoutId[directoryPath] = playerSkin;
                continue;
            }

            var idByte = (byte) id;

            Logger.Info($"Successfully loaded skin in directory: {directoryPath}, given ID: {idByte}");

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
                Logger.Warn("Could not find a valid ID for this skin, perhaps you have used all 255 slots?");
                return;
            }

            var idByte = (byte) id;

            Logger.Info($"Successfully loaded skin in directory: {directoryPath}, given ID: {idByte}");

            // Write the ID to the file and close the StreamWriter
            streamWriter.Write(id);
            streamWriter.Close();

            // Save it in the mapping and overwrite an existing entry
            skins[idByte] = directorySkinPair.Value;
            // Also save the ID in a set so we know it is used now
            idsUsed.Add(idByte);
        }
    }

    /// <summary>
    /// Try to load the textures for a player skin from disk at the given path. This path should be
    /// the full path ending in the directory that contains the texture files.
    /// </summary>
    /// <param name="path">The full path of a directory containing a player skin.</param>
    /// <param name="playerSkin">If the method returns, will contain the loaded player skin or a fallback
    /// empty player skin if no skin could be loaded.</param>
    /// <returns>true if the skin could be loaded, false otherwise.</returns>
    private bool LoadTexturesForSkin(string path, out PlayerSkin playerSkin) {
        // Fallback out value to make sure we can always return false if loading failed
        playerSkin = new PlayerSkin();

        if (!Directory.Exists(path)) {
            return false;
        }

        var knightPath = Path.Combine(path, KnightTextureFileName);
        if (LoadTexture(knightPath, out var knightTexture)) {
            playerSkin.SetKnightTexture(knightTexture);
        }

        var sprintPath = Path.Combine(path, SprintTextureFileName);
        if (LoadTexture(sprintPath, out var sprintTexture)) {
            playerSkin.SetSprintTexture(sprintTexture);
        }

        return true;
    }

    /// <summary>
    /// Try to load a texture at the given file path.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    /// <param name="texture">If the method returns, will contain the loaded texture or null if no texture
    /// could be loaded.</param>
    /// <returns>true if the texture could be loaded, false otherwise.</returns>
    private bool LoadTexture(string filePath, out Texture2D texture) {
        texture = null;

        if (!File.Exists(filePath)) {
            Logger.Warn($"Tried to load texture at: {filePath}, but it didn't exist");
            return false;
        }

        var textureBytes = File.ReadAllBytes(filePath);
        texture = new Texture2D(1, 1);

        return texture.LoadImage(textureBytes, true);
    }

    /// <summary>
    /// Reads an integer from a file at the given path. Returns -1 if the file contents cannot be parsed
    /// to an int.
    /// </summary>
    /// <param name="path">The path of the file to read from.</param>
    /// <returns><code>-1</code> if the file contents cannot be parsed as an int, otherwise the int
    /// value.</returns>
    private static int ReadIntFromFile(string path) {
        var fileContent = File.ReadAllText(path);
        if (!int.TryParse(fileContent, out var id)) {
            return -1;
        }

        return id;
    }

    /// <summary>
    /// Get the mods folder based on the underlying operating system.
    /// </summary>
    /// <returns>The full path to the mods directory.</returns>
    private static string GetModsFolder() {
        switch (SystemInfo.operatingSystemFamily) {
            case OperatingSystemFamily.MacOSX:
                return Path.GetFullPath($"{Application.dataPath}/Resources/Data/Managed/Mods");
            default:
                return Path.GetFullPath($"{Application.dataPath}/Managed/Mods");
        }
    }

    /// <summary>
    /// Combines the variable number of given existing paths into a complete path. Uses Path.Combine
    /// for intermediate steps.
    /// </summary>
    /// <param name="paths">String array containing the path to combine.</param>
    /// <returns>The combined path from the given paths.</returns>
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
