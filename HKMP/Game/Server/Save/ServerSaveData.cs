using System.Collections.Generic;

namespace Hkmp.Game.Server.Save;

/// <summary>
/// Class that holds save data from a server. This consists of global data relating to the world and individual
/// data specific to each player. This class is only used for storing the save data while the server is running;
/// serialization of this data to the save file is done with <see cref="ModSaveFile"/>.
/// </summary>
internal class ServerSaveData {
    /// <summary>
    /// The global save data for the server. E.g. broken walls, open doors, etc.
    /// </summary>
    public Dictionary<ushort, byte[]> GlobalSaveData { get; set; }

    /// <summary>
    /// The player specific save data mapped to player's auth keys.
    /// </summary>
    public Dictionary<string, Dictionary<ushort, byte[]>> PlayerSaveData { get; set; }

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
            return new Dictionary<ushort, byte[]>(GlobalSaveData);
        }

        var saveData = new Dictionary<ushort, byte[]>(GlobalSaveData);
        foreach (var data in playerSaveData) {
            saveData[data.Key] = data.Value;
        }

        return saveData;
    }
}
