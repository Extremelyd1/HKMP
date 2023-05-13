using Hkmp.Game;
using Hkmp.Math;

namespace Hkmp.Api.Server;

/// <summary>
/// A class containing all the relevant data managed by the server about a player.
/// </summary>
public interface IServerPlayer {
    /// <summary>
    /// The ID of the player.
    /// </summary>
    ushort Id { get; }
    
    /// <summary>
    /// The IP address of the player.
    /// </summary>
    string IpAddressString { get; }
    
    /// <summary>
    /// The authentication key of the player.
    /// </summary>
    string AuthKey { get; }

    /// <summary>
    /// Whether this player is authorized, meaning that they have high-level permission.
    /// </summary>
    bool IsAuthorized { get; }

    /// <summary>
    /// The username of the player.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// The name of the scene the player is currently in.
    /// </summary>
    string CurrentScene { get; }

    /// <summary>
    /// The last known position of the player.
    /// </summary>
    Vector2 Position { get; }

    /// <summary>
    /// Whether the player has an active map icon.
    /// </summary>
    bool HasMapIcon { get; }

    /// <summary>
    /// The last known map position of the player.
    /// </summary>
    Vector2 MapPosition { get; }

    /// <summary>
    /// The scale of the player as a bool indicating whether they should be flipped.
    /// </summary>
    bool Scale { get; }

    /// <summary>
    /// The ID of the last animation of the player.
    /// </summary>
    ushort AnimationId { get; }

    /// <summary>
    /// The current team of the player.
    /// </summary>
    Team Team { get; }

    /// <summary>
    /// The ID of the skin of the player.
    /// </summary>
    byte SkinId { get; }
}
