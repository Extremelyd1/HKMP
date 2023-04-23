using System;
using System.Collections.Generic;
using Hkmp.Game;

namespace Hkmp.Api.Client;

/// <summary>
/// Client manager that handles the local client and related data.
/// </summary>
public interface IClientManager {
    /// <summary>
    /// Class that manages player locations on the in-game map.
    /// </summary>
    IMapManager MapManager { get; }
    
    /// <summary>
    /// The current username of the local player.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// The current team of the local player.
    /// </summary>
    Team Team { get; }

    /// <summary>
    /// A read-only collection of all connected players.
    /// </summary>
    IReadOnlyCollection<IClientPlayer> Players { get; }

    /// <summary>
    /// Disconnect the local client from the server.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// Get a specific player by their ID.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <returns>The player with the given ID, or null if no such player exists.</returns>
    IClientPlayer GetPlayer(ushort id);

    /// <summary>
    /// Try to get a specific player by their ID.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="player">The parameter that will contain the player if it exists.</param>
    /// <returns>True if the player was found, false otherwise.</returns>
    bool TryGetPlayer(ushort id, out IClientPlayer player);

    /// <summary>
    /// Changes the team of the local player.
    /// </summary>
    /// <param name="team">The team value.</param>
    void ChangeTeam(Team team);

    /// <summary>
    /// Changes the skin of the local player.
    /// </summary>
    /// <param name="skinId">The ID of the skin.</param>
    void ChangeSkin(byte skinId);

    /// <summary>
    /// Event that is called when the local user connects to a server.
    /// </summary>
    event Action ConnectEvent;

    /// <summary>
    /// Event that is called when the local user disconnects from the server.
    /// </summary>
    event Action DisconnectEvent;

    /// <summary>
    /// Event that is called when another player connects to the server.
    /// </summary>
    event Action<IClientPlayer> PlayerConnectEvent;

    /// <summary>
    /// Event that is called when another player disconnects from the server.
    /// IClientPlayer could possibly be null.
    /// </summary>
    event Action<IClientPlayer> PlayerDisconnectEvent;

    /// <summary>
    /// Event that is called when another player enters the local scene.
    /// </summary>
    event Action<IClientPlayer> PlayerEnterSceneEvent;

    /// <summary>
    /// Event that is called when another player leaves the local scene.
    /// </summary>
    event Action<IClientPlayer> PlayerLeaveSceneEvent;
}
