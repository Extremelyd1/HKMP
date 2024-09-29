using System;
using System.Collections.Generic;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;
using Hkmp.Networking.Server;
using Hkmp.Util;

namespace Hkmp.Game.Command.Server;

/// <summary>
/// Command for allowing players to copy player-specific save data from another player. This is used to catch up
/// to another player's progression by transferring the save data.
/// </summary>
internal class CopySaveCommand : IServerCommand {
    /// <inheritdoc />
    public string Trigger => "/copysave";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc />
    public bool AuthorizedOnly => true;

    /// <summary>
    /// The server manager instance to access players.
    /// </summary>
    private readonly ServerManager _serverManager;

    /// <summary>
    /// The server save data instance for accessing save data to copy.
    /// </summary>
    private readonly ServerSaveData _serverSaveData;

    /// <summary>
    /// The net server instance for networking save data.
    /// </summary>
    private readonly NetServer _netServer;

    public CopySaveCommand(ServerManager serverManager, ServerSaveData serverSaveData, NetServer netServer) {
        _serverManager = serverManager;
        _serverSaveData = serverSaveData;
        _netServer = netServer;
    }

    /// <inheritdoc />
    public void Execute(ICommandSender commandSender, string[] args) {
        if (args.Length < 3) {
            commandSender.SendMessage($"Invalid usage: {Trigger} <from username> <to username>");
            return;
        }

        var fromUsername = args[1];
        if (!CommandUtil.TryGetPlayerByName(_serverManager.Players, fromUsername, out var fromPlayer)) {
            commandSender.SendMessage($"Could not find player with name '{fromUsername}'");
            return;
        }

        var toUsername = args[2];
        if (!CommandUtil.TryGetPlayerByName(_serverManager.Players, toUsername, out var toPlayer)) {
            commandSender.SendMessage($"Could not find player with name '{toUsername}'");
            return;
        }

        var toCopyData = new Dictionary<ushort, byte[]>(_serverSaveData.PlayerSaveData[fromPlayer.AuthKey]);

        _serverSaveData.PlayerSaveData[toPlayer.AuthKey] = toCopyData;

        foreach (var saveEntry in toCopyData) {
            _netServer.GetUpdateManagerForClient(toPlayer.Id).SetSaveUpdate(saveEntry.Key, saveEntry.Value);
        }
        
        commandSender.SendMessage($"Copied player save file from '{fromUsername}' to '{toUsername}'");
    }
}
