using Hkmp.Game;
using UnityEngine;

namespace Hkmp.Api.Client;

/// <summary>
/// A class containing all the relevant data managed by the client about a player.
/// </summary>
public interface IClientPlayer {
    /// <summary>
    /// The ID of the player.
    /// </summary>
    ushort Id { get; }

    /// <summary>
    /// The username of the player.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Whether the player is in our local scene.
    /// </summary>
    bool IsInLocalScene { get; }

    /// <summary>
    /// The Unity game object for the player container. This container contains all player relevant
    /// game objects, such as the player object, effects, spells, animations etc.
    /// </summary>
    GameObject PlayerContainer { get; }

    /// <summary>
    /// The Unity game object for the player object.
    /// </summary>
    GameObject PlayerObject { get; }

    /// <summary>
    /// The current team of the player.
    /// </summary>
    Team Team { get; }

    /// <summary>
    /// The ID of the current skin of the player.
    /// </summary>
    byte SkinId { get; }
}
