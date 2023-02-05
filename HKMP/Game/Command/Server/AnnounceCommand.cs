using System;
using System.Collections.Concurrent;
using Hkmp.Api.Command.Server;
using Hkmp.Concurrency;
using Hkmp.Game.Server;
using Hkmp.Networking.Server;

namespace Hkmp.Game.Command.Server;

/// <summary>
/// Command for announcing messages to all connected players.
/// </summary>
internal class AnnounceCommand : IServerCommand {
    /// <inheritdoc />
    public string Trigger => "/announce";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc />
    public bool AuthorizedOnly => true;

    /// <summary>
    /// A reference to the server player data dictionary.
    /// </summary>
    private readonly ConcurrentDictionary<ushort, ServerPlayerData> _playerData;

    /// <summary>
    /// The net server instance.
    /// </summary>
    private readonly NetServer _netServer;

    public AnnounceCommand(ConcurrentDictionary<ushort, ServerPlayerData> playerData, NetServer netServer) {
        _playerData = playerData;
        _netServer = netServer;
    }

    /// <inheritdoc />
    public void Execute(ICommandSender commandSender, string[] args) {
        if (args.Length < 2) {
            commandSender.SendMessage($"Invalid usage: {Trigger} <message>");
            return;
        }

        var message = $"<SERVER>: {string.Join(" ", args).Substring(Trigger.Length + 1)}";

        foreach (var playerData in _playerData.Values) {
            _netServer.GetUpdateManagerForClient(playerData.Id).AddChatMessage(message);
        }
    }
}
