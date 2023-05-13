using System;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;
using Hkmp.Game.Server.Auth;
using Hkmp.Util;

namespace Hkmp.Game.Command.Server;

/// <summary>
/// Command for managing the white-list.
/// </summary>
internal class WhiteListCommand : IServerCommand {
    /// <inheritdoc />
    public string Trigger => "/whitelist";

    /// <inheritdoc />
    public string[] Aliases => Array.Empty<string>();

    /// <inheritdoc />
    public bool AuthorizedOnly => true;

    /// <summary>
    /// The white-list instance.
    /// </summary>
    private readonly WhiteList _whiteList;

    /// <summary>
    /// The server manager instance.
    /// </summary>
    private readonly ServerManager _serverManager;

    public WhiteListCommand(WhiteList whiteList, ServerManager serverManager) {
        _whiteList = whiteList;
        _serverManager = serverManager;
    }

    /// <inheritdoc />
    public void Execute(ICommandSender commandSender, string[] args) {
        if (args.Length < 2) {
            SendUsage(commandSender);
            return;
        }

        var action = args[1].ToLower();

        if (action == "on") {
            if (_whiteList.IsEnabled) {
                commandSender.SendMessage("Whitelist was already enabled");
                return;
            }

            _whiteList.IsEnabled = true;
            commandSender.SendMessage("Whitelist has been enabled");
        } else if (action == "off") {
            if (!_whiteList.IsEnabled) {
                commandSender.SendMessage("Whitelist was already disabled");
                return;
            }

            _whiteList.IsEnabled = false;
            commandSender.SendMessage("Whitelist has been disabled");
        } else if (action == "add" || action == "remove") {
            if (args.Length < 3) {
                commandSender.SendMessage($"Invalid usage: {Trigger} <add|remove> <auth key|username>");
                return;
            }

            // Store whether the action is to add or whether to remove
            var addAction = action == "add";

            var identifier = args[2];
            if (AuthUtil.IsValidAuthKey(identifier)) {
                if (addAction) {
                    _whiteList.Add(identifier);
                    commandSender.SendMessage("Auth key has been added to whitelist");
                } else {
                    _whiteList.Remove(identifier);
                    commandSender.SendMessage("Auth key has been removed from whitelist");
                }
            } else {
                // Since the given argument was not a valid auth key, we try player names instead
                if (!CommandUtil.TryGetPlayerByName(_serverManager.Players, identifier, out var player)) {
                    if (addAction) {
                        _whiteList.AddPreList(identifier);
                        commandSender.SendMessage(
                            $"Added player name '{identifier}' to pre-list. The next player that logs in with that name will be whitelisted.");
                    } else {
                        _whiteList.RemovePreList(identifier);
                        commandSender.SendMessage(
                            $"Removed player name '{identifier}' from pre-list. The next player that logs in with that name will no longer be whitelisted.");
                    }

                    return;
                }

                var playerData = (ServerPlayerData) player;

                if (addAction) {
                    _whiteList.Add(playerData.AuthKey);
                    commandSender.SendMessage(
                        $"Auth key of player '{player.Username}' has been added to whitelist");
                } else {
                    _whiteList.Remove(playerData.AuthKey);
                    commandSender.SendMessage(
                        $"Auth key of player '{player.Username}' has been removed from whitelist");
                }
            }
        } else if (action == "prelist") {
            commandSender.SendMessage($"Pre-listed player names: {_whiteList.GetPreListed()}");
        } else if (action == "clear") {
            if (args.Length < 3) {
                _whiteList.Clear();
                commandSender.SendMessage("Cleared whitelist");
                return;
            }

            if (args[2] == "prelist") {
                _whiteList.ClearPreList();
                commandSender.SendMessage("Clear pre-list of whitelist");
            } else {
                commandSender.SendMessage($"Invalid usage: '{Trigger} clear prelist' to clear pre-list");
            }
        } else {
            SendUsage(commandSender);
        }
    }

    /// <summary>
    /// Send the general command usage to the given command sender.
    /// </summary>
    /// <param name="commandSender">The command sender to send to.</param>
    private void SendUsage(ICommandSender commandSender) {
        commandSender.SendMessage($"Invalid usage: {Trigger} <on|off|add|remove|prelist|clear> [arguments]");
    }
}
