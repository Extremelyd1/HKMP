
namespace Hkmp.Api.Client; 

/// <summary>
/// Map manager that handles data related to local map icons.
/// </summary>
public interface IMapManager {
    /// <summary>
    /// Try to get a map entry by a player's ID.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="playerMapEntry">The parameter that will contain the map entry if it exists.</param>
    /// <returns>True if the map entry was found, false otherwise.</returns>
    public bool TryGetEntry(ushort id, out IPlayerMapEntry playerMapEntry);
}
