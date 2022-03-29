using System.Linq;
using System.Net;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;
using Hkmp.Game.Server.Auth;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;

namespace Hkmp.Game.Command.Server {
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

            // If the command is not an unban, we check whether the second argument is a special argument
            if (!unban) {
                if (identifier == "clear") {
                    if (ipBan) {
                        _banList.ClearIps();
                        commandSender.SendMessage("Cleared IP address from ban list");
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
                        commandSender.SendMessage(
                            $"Invalid usage: {args[0]} add <auth key|username{(ipBan ? "|ip address" : "")}>");
                        return;
                    }

                    identifier = args[2];
                }
            }

            // Cast each element in the collection of players to ServerPlayerData
            var players = _serverManager.Players.Select(p => (ServerPlayerData)p).ToList();

            // Check if the identifier argument is an authentication key, which by definition means that it can't
            // be a player name or IP address
            if (AuthUtil.IsValidAuthKey(identifier)) {
                var foundPlayerWithAuthKey = CommandUtil.TryGetPlayerByAuthKey(
                    players,
                    identifier,
                    out var playerWithAuthKey
                );

                if (!ipBan) {
                    if (unban) {
                        _banList.Remove(identifier);
                        commandSender.SendMessage("Auth key has been unbanned");
                    } else {
                        _banList.Add(identifier);
                        commandSender.SendMessage("Auth key has been banned");

                        if (foundPlayerWithAuthKey) {
                            DisconnectPlayer(playerWithAuthKey);
                        }
                    }

                    return;
                }

                if (!foundPlayerWithAuthKey) {
                    commandSender.SendMessage($"Could not find player with given auth key");
                    return;
                }

                if (unban) {
                    _banList.RemoveIp(playerWithAuthKey.IpAddressString);
                    commandSender.SendMessage($"IP address '{playerWithAuthKey.IpAddressString}' has been unbanned");
                } else {
                    _banList.AddIp(playerWithAuthKey.IpAddressString);
                    commandSender.SendMessage($"IP address '{playerWithAuthKey.IpAddressString}' has been banned");

                    DisconnectPlayer(playerWithAuthKey);
                }

                return;
            }

            if (CommandUtil.TryGetPlayerByName(_serverManager.Players, identifier, out var player)) {
                var playerData = (ServerPlayerData)player;

                if (ipBan) {
                    if (unban) {
                        _banList.RemoveIp(playerData.IpAddressString);
                        commandSender.SendMessage($"IP address of player '{playerData.Username}' has been unbanned");
                    } else {
                        _banList.AddIp(playerData.IpAddressString);
                        commandSender.SendMessage($"IP address of player '{playerData.Username}' has been banned");

                        DisconnectPlayer(playerData);
                    }

                    return;
                }

                if (unban) {
                    _banList.Remove(playerData.AuthKey);
                    commandSender.SendMessage($"Player '{playerData.Username}' has been unbanned");
                } else {
                    _banList.Add(playerData.AuthKey);
                    commandSender.SendMessage($"Player '{playerData.Username}' has been banned");

                    DisconnectPlayer(playerData);
                }

                return;
            }

            if (!ipBan) {
                commandSender.SendMessage($"Could not find player with name or auth key '{identifier}'");
                return;
            }

            if (!IPAddress.TryParse(identifier, out var address)) {
                commandSender.SendMessage($"Could not find player with name, auth key or IP address '{identifier}'");
                return;
            }

            if (unban) {
                _banList.RemoveIp(address.ToString());
                commandSender.SendMessage($"IP address '{identifier}' has been unbanned");
            } else {
                _banList.AddIp(address.ToString());
                commandSender.SendMessage($"IP address '{identifier}' has been banned");

                var playerWithIp = players.Find(p => p.IpAddressString == address.ToString());
                if (playerWithIp != null) {
                    DisconnectPlayer(playerWithIp);
                }
            }
        }

        /// <summary>
        /// Disconnect the player with the given player data.
        /// </summary>
        /// <param name="playerData">The player data for the player to disconnect.</param>
        private void DisconnectPlayer(ServerPlayerData playerData) => _serverManager.DisconnectPlayer(
            playerData.Id,
            DisconnectReason.Banned
        );

        /// <summary>
        /// Sends the command usage to the given command sender.
        /// </summary>
        /// <param name="commandSender">The command sender to send to.</param>
        /// <param name="ipBan">Whether the command was for an IP ban.</param>
        /// <param name="unban">Whether the command was for an unban.</param>
        private void SendUsage(ICommandSender commandSender, bool ipBan, bool unban) {
            var cmd = $"/{(unban ? "un" : "")}ban{(ipBan ? "ip" : "")}";

            commandSender.SendMessage($"Invalid usage: {cmd} <auth key|username{(ipBan ? "|ip address" : "")}>");
        }
    }
}