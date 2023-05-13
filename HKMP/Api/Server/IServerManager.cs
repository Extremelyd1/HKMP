using System;
using System.Collections.Generic;
using Hkmp.Api.Eventing.ServerEvents;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Api.Server;

/// <summary>
/// The server manager that handles server state.
/// </summary>
public interface IServerManager {
    /// <summary>
    /// A read-only collection of all connected players.
    /// </summary>
    IReadOnlyCollection<IServerPlayer> Players { get; }

    /// <summary>
    /// A read-only <see cref="ServerSettings"/> that contains the settings related to gameplay.
    /// </summary>
    IServerSettings ServerSettings { get; }

    /// <summary>
    /// Get a specific player by their ID.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <returns>The player with the given ID, or null if no such player exists.</returns>
    IServerPlayer GetPlayer(ushort id);

    /// <summary>
    /// Try to get a specific player by their ID.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="player">The parameter that will contain the player if it exists.</param>
    /// <returns>True if the player was found, false otherwise.</returns>
    bool TryGetPlayer(ushort id, out IServerPlayer player);

    /// <summary>
    /// Send a message to the player with the given ID.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="message">The message as a string with length &lt; 256.</param>
    /// <exception cref="ArgumentException">Thrown if a player with the given ID could not be found,
    /// the message is null, the length of the message is greater than 255 or the message contains
    /// invalid characters.</exception>
    void SendMessage(ushort id, string message);

    /// <summary>
    /// Send a message to the given player.
    /// </summary>
    /// <param name="player">The player to send to.</param>
    /// <param name="message">The message as a string with length &lt; 256.</param>
    /// <exception cref="ArgumentException">Thrown if the given player is null, the message is null, the length
    /// of the message is greater than 255 or the message contains invalid characters.</exception>
    void SendMessage(IServerPlayer player, string message);

    /// <summary>
    /// Broadcast a message to all connected players.
    /// </summary>
    /// <param name="message">The message as a string with length &lt; 256.</param>
    /// <exception cref="ArgumentException">Thrown if the message is null or the length of the message is
    /// greater than 255 or the message contains invalid characters.</exception>
    void BroadcastMessage(string message);

    /// <summary>
    /// Disconnect the player with the given ID for the given reason.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="reason">The reason for the disconnect.</param>
    void DisconnectPlayer(ushort id, DisconnectReason reason);

    /// <summary>
    /// Apply the given <see cref="ServerSettings"/> to the game. Will copy all property values to the used
    /// <see cref="ServerSettings"/> instance and network the changes to all players.
    /// </summary>
    /// <param name="serverSettings">The <see cref="ServerSettings"/> to apply.</param>
    void ApplyServerSettings(ServerSettings serverSettings);

    /// <summary>
    /// Event that is called when a player connects to the server.
    /// </summary>
    event Action<IServerPlayer> PlayerConnectEvent;

    /// <summary>
    /// Event that is called when a player disconnects from the server.
    /// </summary>
    event Action<IServerPlayer> PlayerDisconnectEvent;

    /// <summary>
    /// Event that is called when a player enters a scene.
    /// </summary>
    event Action<IServerPlayer> PlayerEnterSceneEvent;

    /// <summary>
    /// Event that is called when a players leaves a scene.
    /// </summary>
    event Action<IServerPlayer> PlayerLeaveSceneEvent;

    /// <summary>
    /// Event that is called when a player sends a chat message.
    /// </summary>
    event Action<IPlayerChatEvent> PlayerChatEvent;
}
