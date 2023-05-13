using System.Linq;
using System.Net;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;
using Hkmp.Game.Server.Auth;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;

namespace Hkmp.Game.Command.Server;

/// <summary>
/// Command for banning users.
/// </summary>
internal class BanCommand : IServerCommand {
    /// <inheritdoc />
    public string Trigger => "/ban";

    /// <inheritdoc />
    public string[] Aliases => new[] { "/unban", "/banip", "/unbanip" };

    /// <inheritdoc />
    public bool AuthorizedOnly => true;

    /// <summary>
    /// The ban list instance.
    /// </summary>
    private readonly BanList _banList;

    /// <summary>
    /// The server manager instance.
    /// </summary>
    private readonly ServerManager _serverManager;

    public BanCommand(BanList banList, ServerManager serverManager) {
        _banList = banList;
        _serverManager = serverManager;
    }

    /// <inheritdoc />
    public void Execute(ICommandSender commandSender, string[] args) {
        var ipBan = args[0].Contains("ip");
        var unban = args[0].Contains("unban");

        if (args.Length < 2) {
            SendUsage(commandSender, ipBan, unban);
            return;
        }

        var identifier = args[1];
        IPAddress address;

        if (unban) {
            if (ipBan) {
                if (!IPAddress.TryParse(identifier, out address)) {
                    commandSender.SendMessage("Given argument is not a valid IP address");
                    return;
                }

                _banList.RemoveIp(address.ToString());
                commandSender.SendMessage($"IP address '{identifier}' has been unbanned");
            } else {
                if (!AuthUtil.IsValidAuthKey(identifier)) {
                    commandSender.SendMessage("Given argument is not a valid authentication key");
                    return;
                }

                _banList.Remove(identifier);
                commandSender.SendMessage("Auth key has been unbanned");
            }

            return;
        }

        // If the command is not an unban, we check whether the second argument is a special argument
        if (identifier == "clear") {
            if (ipBan) {
                _banList.ClearIps();
                commandSender.SendMessage("Cleared IP addresses from ban list");
            } else {
                _banList.Clear();
                commandSender.SendMessage("Cleared auth keys from ban list");
            }

            return;
        }

        // To make sure we can still (ip) ban a player that has the name "clear",
        // we add an intermediate argument
        if (identifier == "add") {
            if (args.Length < 3) {
                SendUsage(commandSender, ipBan, false, true);
                return;
            }

            identifier = args[2];
        }

        // Cast each element in the collection of players to ServerPlayerData
        var players = _serverManager.Players.Select(p => (ServerPlayerData) p).ToList();

        // Check if the identifier argument is an authentication key, which by definition means that it can't
        // be a player name or IP address
        if (AuthUtil.IsValidAuthKey(identifier)) {
            var foundPlayerWithAuthKey = CommandUtil.TryGetPlayerByAuthKey(
                players,
                identifier,
                out var playerWithAuthKey
            );

            // First check if this is not an IP ban, because then we simply add the auth key to the ban list
            if (!ipBan) {
                _banList.Add(identifier);
                commandSender.SendMessage("Auth key has been banned");

                if (foundPlayerWithAuthKey) {
                    DisconnectPlayer(playerWithAuthKey);
                }

                return;
            }

            // If it is an IP ban, we can only issue it if a player with that auth key is online
            if (!foundPlayerWithAuthKey) {
                commandSender.SendMessage($"Could not find player with given auth key");
                return;
            }

            _banList.AddIp(playerWithAuthKey.IpAddressString);
            commandSender.SendMessage($"IP address '{playerWithAuthKey.IpAddressString}' has been banned");

            DisconnectPlayer(playerWithAuthKey);

            return;
        }

        // Now we check whether the argument supplied is the username of a player
        if (CommandUtil.TryGetPlayerByName(_serverManager.Players, identifier, out var player)) {
            var playerData = (ServerPlayerData) player;

            // Based on whether it is an IP ban or not, add it to the correct ban list and inform the
            // command sender of the behaviour
            if (ipBan) {
                _banList.AddIp(playerData.IpAddressString);
                commandSender.SendMessage($"IP address of player '{playerData.Username}' has been banned");
            } else {
                _banList.Add(playerData.AuthKey);
                commandSender.SendMessage($"Player '{playerData.Username}' has been banned");
            }

            DisconnectPlayer(playerData);

            return;
        }

        // If it is not an IP ban, we have not found the user that was targeted by the identifier
        if (!ipBan) {
            commandSender.SendMessage($"Could not find player with name or auth key '{identifier}'");
            return;
        }

        // Now we can check whether the argument was an IP address
        if (!IPAddress.TryParse(identifier, out address)) {
            commandSender.SendMessage($"Could not find player with name, auth key or IP address '{identifier}'");
            return;
        }

        _banList.AddIp(address.ToString());
        commandSender.SendMessage($"IP address '{identifier}' has been banned");

        // If a player with the given IP is connected, disconnect them
        if (CommandUtil.TryGetPlayerByIpAddress(players, address.ToString(), out var playerWithIp)) {
            DisconnectPlayer(playerWithIp);
        }
    }

    /// <summary>
    /// Disconnect the player with the given player data.
    /// </summary>
    /// <param name="playerData">The player data for the player to disconnect.</param>
    private void DisconnectPlayer(ServerPlayerData playerData) => _serverManager.InternalDisconnectPlayer(
        playerData.Id,
        DisconnectReason.Banned
    );

    /// <summary>
    /// Sends the command usage to the given command sender.
    /// </summary>
    /// <param name="commandSender">The command sender to send to.</param>
    /// <param name="ipBan">Whether the command was for an IP ban.</param>
    /// <param name="unban">Whether the command was for an unban.</param>
    /// <param name="addArgument">Whether the 'add' argument was supplied.</param>
    private void SendUsage(ICommandSender commandSender, bool ipBan, bool unban, bool addArgument = false) {
        if (ipBan) {
            if (unban) {
                commandSender.SendMessage($"{Aliases[2]} <ip address>");
            } else {
                commandSender.SendMessage(
                    $"{Aliases[1]} {(addArgument ? "add" : "")} <auth key|username|ip address>");
            }
        } else {
            if (unban) {
                commandSender.SendMessage($"{Aliases[0]} <auth key>");
            } else {
                commandSender.SendMessage($"{Trigger} {(addArgument ? "add" : "")} <auth key|username>");
            }
        }
    }
}
