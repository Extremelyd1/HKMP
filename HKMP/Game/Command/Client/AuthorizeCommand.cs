using Hkmp.Api.Command.Server;
using Hkmp.Concurrency;
using Hkmp.Game.Server;
using Hkmp.Util;

namespace Hkmp.Game.Command.Client {
    public class AuthorizeCommand : IServerCommand {
        public string Trigger => "/auth";
        public string[] Aliases => new[] { "/deauth", "/authorize", "/deauthorize" };
        public bool AuthorizedOnly => true;

        private readonly AuthList _authorizedList;

        public AuthorizeCommand(AuthList authorizedList) {
            _authorizedList = authorizedList;
        }

        public void Execute(ICommandSender commandSender, string[] args) {
            if (args.Length < 2) {
                SendUsage(commandSender);
                return;
            }

            var action = args[0];
            var authKey = args[1];
            
            if (!AuthUtil.IsValidAuthKey(authKey)) {
                commandSender.SendMessage("Given auth key is invalid");
                return;
            }

            if (action.Contains("deauth")) {
                _authorizedList.Remove(authKey);

                commandSender.SendMessage("Auth key has been deauthorized");
            } else {
                _authorizedList.Add(authKey);
                commandSender.SendMessage("Auth key has been authorized");
            }
        }

        private void SendUsage(ICommandSender commandSender) {
            commandSender.SendMessage($"Invalid usage: <{Trigger}|{Aliases[0]}> <auth key>");
        }
    }
}