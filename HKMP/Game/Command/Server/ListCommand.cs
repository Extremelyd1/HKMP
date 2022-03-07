using System;
using System.Linq;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;

namespace Hkmp.Game.Command.Server {
    public class ListCommand : IServerCommand {
        public string Trigger => "/list";
        public string[] Aliases => Array.Empty<string>();
        public bool AuthorizedOnly => false;

        private readonly ServerManager _serverManager;

        public ListCommand(ServerManager serverManager) {
            _serverManager = serverManager;
        }

        public void Execute(ICommandSender commandSender, string[] arguments) {
            var players = _serverManager.Players;

            var playerNames = string.Join(", ", players.Select(p => p.Username));
            
            commandSender.SendMessage($"Online players ({players.Count}): {playerNames}");
        }
    }
}