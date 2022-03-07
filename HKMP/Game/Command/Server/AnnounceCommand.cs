using System;
using Hkmp.Api.Command.Server;
using Hkmp.Concurrency;
using Hkmp.Game.Server;
using Hkmp.Networking.Server;

namespace Hkmp.Game.Command.Server {
    public class AnnounceCommand : IServerCommand {
        public string Trigger => "/announce";
        public string[] Aliases => Array.Empty<string>();
        public bool AuthorizedOnly => true;

        private readonly ConcurrentDictionary<ushort, ServerPlayerData> _playerData;
        private readonly NetServer _netServer;

        public AnnounceCommand(ConcurrentDictionary<ushort, ServerPlayerData> playerData, NetServer netServer) {
            _playerData = playerData;
            _netServer = netServer;
        }

        public void Execute(ICommandSender commandSender, string[] args) {
            if (args.Length < 2) {
                commandSender.SendMessage($"Invalid usage: {Trigger} <message>");
                return;
            }

            var message = $"<SERVER>: {string.Join(" ", args).Substring(Trigger.Length + 1)}";

            foreach (var playerData in _playerData.GetCopy().Values) {
                _netServer.GetUpdateManagerForClient(playerData.Id).AddChatMessage(message);
            }
        }
    }
}