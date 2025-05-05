using System.Collections.Generic;
using Hkmp.Game.Client.Save;
using Hkmp.Logging;

namespace Hkmp.Game.Server.Save;

/// <summary>
/// Class that holds save data from a server. This consists of global data relating to the world and individual
/// data specific to each player. This class is only used for storing the save data while the server is running;
/// serialization of this data to the save file is done with <see cref="ModSaveFile"/>.
/// </summary>
internal class ServerSaveData {
    /// <summary>
    /// Name of the variable in PlayerData that denotes a Steel Soul save file.
    /// </summary>
    private const string SteelSoulVarName = "permadeathMode";
    /// <summary>
    /// Name of the variable in PlayerData that denotes a Godseeker save file.
    /// </summary>
    private const string GodseekerVarName = "bossRushMode";

    /// <summary>
    /// The index that corresponds with the Steel Soul variable.
    /// </summary>
    private static ushort _steelSoulIndex;
    /// <summary>
    /// The index that corresponds with the Godseeker variable.
    /// </summary>
    private static ushort _godseekerIndex;
    
    /// <summary>
    /// The global save data for the server. E.g. broken walls, open doors, etc.
    /// </summary>
    public Dictionary<ushort, byte[]> GlobalSaveData { get; set; }

    /// <summary>
    /// The player specific save data mapped to player's auth keys.
    /// </summary>
    public Dictionary<string, Dictionary<ushort, byte[]>> PlayerSaveData { get; set; }

    /// <summary>
    /// Static constructor for initializing the indices for the Steel Soul and Godseeker variables.
    /// </summary>
    static ServerSaveData() {
        if (!SaveDataMapping.Instance.PlayerDataIndices.TryGetValue(SteelSoulVarName, out _steelSoulIndex)) {
            Logger.Warn("Could not find index for steel soul variable");
        }
        
        if (!SaveDataMapping.Instance.PlayerDataIndices.TryGetValue(GodseekerVarName, out _godseekerIndex)) {
            Logger.Warn("Could not find index for godseeker variable");
        }
    }
    
    public ServerSaveData() {
        GlobalSaveData = new Dictionary<ushort, byte[]>();
        PlayerSaveData = new Dictionary<string, Dictionary<ushort, byte[]>>();
    }

    /// <summary>
    /// Get merged save data that contains global save data and player specific save data for the player with the
    /// given auth key. 
    /// </summary>
    /// <param name="authKey">The auth key that corresponds to the player for the player specific data.</param>
    /// <returns>A dictionary mapping save data indices to byte encoded values.</returns>
    public Dictionary<ushort, byte[]> GetMergedSaveData(string authKey) {
        if (!PlayerSaveData.TryGetValue(authKey, out var playerSaveData)) {
            if (IsGodseeker(GlobalSaveData)) {
                Logger.Debug("Global server data indicates Godseeker mode, adding overrides for new player data");
                // Obtain a new dictionary with the Godseeker overrides
                playerSaveData = new Dictionary<ushort, byte[]>(SaveManager.GodseekerOverrides);

                // And immediately store it in the player save data dictionary for future use
                PlayerSaveData[authKey] = playerSaveData;
            } else {
                playerSaveData = new Dictionary<ushort, byte[]>();
            }
        }

        var saveData = new Dictionary<ushort, byte[]>(GlobalSaveData);
        foreach (var data in playerSaveData) {
            saveData[data.Key] = data.Value;
        }

        return saveData;
    }

    /// <summary>
    /// Whether the given global save data is for Steel Soul mode.
    /// </summary>
    /// <param name="globalSaveData">The dictionary containing the global save data.</param>
    /// <returns>True if the save data is for Steel Soul mode, false otherwise.</returns>
    private static bool IsSteelSoul(Dictionary<ushort, byte[]> globalSaveData) {
        if (!globalSaveData.TryGetValue(_steelSoulIndex, out var value)) {
            return false;
        }

        return value.Length > 0 && value[0] == 1;
    }

    /// <summary>
    /// Whether the given global save data is for Godseeker mode.
    /// </summary>
    /// <param name="globalSaveData">The dictionary containing the global save data.</param>
    /// <returns>True if the save data is for Godseeker mode, false otherwise.</returns>
    private static bool IsGodseeker(Dictionary<ushort, byte[]> globalSaveData) {
        if (!globalSaveData.TryGetValue(_godseekerIndex, out var value)) {
            return false;
        }

        return value.Length > 0 && value[0] == 1;
    }
}
