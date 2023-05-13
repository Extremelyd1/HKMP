using System;
using System.Linq;
using Hkmp.Api.Command.Server;
using Hkmp.Api.Server;
using Hkmp.Game.Server;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;

namespace Hkmp.Game.Command.Server;

/// <summary>
/// Command for kicking users.
/// </summary>
internal class KickCommand : IServerCommand {
    /// <inheritdoc />
    public string Trigger => "/kick";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc />
    public bool AuthorizedOnly => true;

    /// <summary>
    /// The server manager instance.
    /// </summary>
    private readonly ServerManager _serverManager;

    public KickCommand(ServerManager serverManager) {
        _serverManager = serverManager;
    }

    /// <inheritdoc />
    public void Execute(ICommandSender commandSender, string[] args) {
        if (args.Length < 2) {
            commandSender.SendMessage($"Invalid usage: {Trigger} <auth key|username|ip address>");
            return;
        }

        var identifier = args[1];

        // Cast each element in the collection of players to ServerPlayerData
        var players = _serverManager.Players.Select(p => (ServerPlayerData) p).ToList();

        // Check if the identifier argument is an authentication key, which by definition means that it can't
        // be a player name or IP address
        if (AuthUtil.IsValidAuthKey(identifier)) {
            if (!CommandUtil.TryGetPlayerByAuthKey(players, identifier, out var playerWithAuthKey)) {
                commandSender.SendMessage("Could not find player with given auth key");
                return;
            }

            commandSender.SendMessage("Player with auth key has been kicked");
            KickPlayer(playerWithAuthKey);
            return;
        }

        // Check if a player is connected that has the same IP as the given argument
        if (CommandUtil.TryGetPlayerByIpAddress(players, identifier, out var playerWithIp)) {
            commandSender.SendMessage($"Player with IP '{identifier}' has been kicked");
            KickPlayer(playerWithIp);
            return;
        }

        // Check if a player is connected with the same name as the given argument
        if (CommandUtil.TryGetPlayerByName(_serverManager.Players, identifier, out var playerWithName)) {
            commandSender.SendMessage($"Player '{playerWithName.Username}' has been kicked");
            KickPlayer(playerWithName);
            return;
        }

        commandSender.SendMessage($"Could not find player with name, auth key or IP address '{identifier}'");
    }

    /// <summary>
    /// Disconnects the player with the given player data by kicking them.
    /// </summary>
    /// <param name="player">The server player data instance for the player.</param>
    private void KickPlayer(IServerPlayer player) {
        _serverManager.InternalDisconnectPlayer(player.Id, DisconnectReason.Kicked);
    }
}
