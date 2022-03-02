using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;
using Hkmp.Game.Server.Auth;
using Hkmp.Util;

namespace Hkmp.Game.Command.Server {
    public class AuthorizeCommand : IServerCommand {
        public string Trigger => "/auth";
        public string[] Aliases => new[] { "/deauth", "/authorize", "/deauthorize" };
        public bool AuthorizedOnly => true;

        private readonly AuthorizedList _authorizedList;
        private readonly ServerManager _serverManager;

        public AuthorizeCommand(AuthorizedList authorizedList, ServerManager serverManager) {
            _authorizedList = authorizedList;
            _serverManager = serverManager;
        }

        public void Execute(ICommandSender commandSender, string[] args) {
            if (args.Length < 2) {
                SendUsage(commandSender);
                return;
            }

            // Store whether the action is to authorize or whether to de-authorize
            var authAction = !args[0].Contains("deauth");
            var identifier = args[1];
            
            if (AuthUtil.IsValidAuthKey(identifier)) {
                if (authAction) {
                    _authorizedList.Add(identifier);
                    commandSender.SendMessage("Auth key has been authorized");
                } else {
                    _authorizedList.Remove(identifier);
                    commandSender.SendMessage("Auth key has been deauthorized");
                }
            } else {
                // Since the given argument was not a valid auth key, we try player names instead
                if (!CommandUtil.TryGetPlayerByName(_serverManager.Players, identifier, out var player)) {
                    commandSender.SendMessage($"Could not find player with name '{identifier}'");
                    return;
                }

                var playerData = (ServerPlayerData)player;

                if (authAction) {
                    _authorizedList.Add(playerData.AuthKey);
                    commandSender.SendMessage($"Auth key of player '{player.Username}' has been authorized");
                } else {
                    _authorizedList.Remove(playerData.AuthKey);
                    commandSender.SendMessage($"Auth key of player '{player.Username}' has been de-authorized");
                }
            }
        }

        private void SendUsage(ICommandSender commandSender) {
            commandSender.SendMessage($"Invalid usage: <{Trigger}|{Aliases[0]}> <auth key|username>");
        }
    }
}