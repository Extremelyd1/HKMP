using Hkmp.Api.Command;
using Hkmp.Api.Command.Server;
using Hkmp.Networking.Server;

namespace Hkmp.Game.Server;

/// <summary>
/// Specialization of a command sender that is a connected player.
/// </summary>
internal class PlayerCommandSender : IPlayerCommandSender {
    /// <inheritdoc />
    public bool IsAuthorized { get; }

    /// <inheritdoc />
    public CommandSenderType Type => CommandSenderType.Player;
    
    /// <inheritdoc />
    public ushort Id { get; }

    /// <summary>
    /// The update manager corresponding to this player for sending messages.
    /// </summary>
    private readonly ServerUpdateManager _updateManager;

    /// <summary>
    /// Construct the player command sender.
    /// </summary>
    /// <param name="isAuthorized">Whether the player is authorized on the server.</param>
    /// <param name="id">The ID of the player.</param>
    /// <param name="updateManager">The update manager for the player.</param>
    public PlayerCommandSender(bool isAuthorized, ushort id, ServerUpdateManager updateManager) {
        IsAuthorized = isAuthorized;
        Id = id;

        _updateManager = updateManager;
    }

    /// <inheritdoc />
    public void SendMessage(string message) {
        _updateManager.AddChatMessage(message);
    }
}
