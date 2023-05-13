using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Api.Client; 

/// <summary>
/// An entry for an icon of a player.
/// </summary>
public interface IPlayerMapEntry {
    /// <summary>
    /// Whether the player has an icon.
    /// </summary>
    public bool HasMapIcon { get; }

    /// <summary>
    /// The position of the icon.
    /// </summary>
    public Vector2 Position { get; }
}
