using System;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;
using Hkmp.Util;

namespace Hkmp.Game.Command.Server {
    public class WhiteListCommand : IServerCommand {
        public string Trigger => "/whitelist";
        public string[] Aliases => Array.Empty<string>();
        public bool AuthorizedOnly => true;

        private readonly AuthList _whiteList;

        public WhiteListCommand(AuthList whiteList) {
            _whiteList = whiteList;
        }

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
                    SendUsage(commandSender);
                    return;
                }

                var authKey = args[2];
                if (!AuthUtil.IsValidAuthKey(authKey)) {
                    commandSender.SendMessage("Given auth key is invalid");
                    return;
                }

                if (action == "add") {
                    _whiteList.Add(authKey);
                    commandSender.SendMessage("Auth key has been added to whitelist");
                } else if (action == "remove") {
                    _whiteList.Remove(authKey);
                    commandSender.SendMessage("Auth key has been removed from whitelist");
                }
            } else {
                SendUsage(commandSender);
            }
        }

        private void SendUsage(ICommandSender commandSender) {
            commandSender.SendMessage($"Invalid usage: {Trigger} <on|off|add|remove> [auth key]");
        }
    }
}