using Hkmp.Api.Command.Server;
using Hkmp.Networking.Server;

namespace Hkmp.Game.Server {
    public class PlayerCommandSender : ICommandSender {
        public bool IsAuthorized { get; }
        public CommandSenderType Type => CommandSenderType.Player;

        private readonly ServerUpdateManager _updateManager;

        public PlayerCommandSender(bool isAuthorized, ServerUpdateManager updateManager) {
            IsAuthorized = isAuthorized;

            _updateManager = updateManager;
        }
        
        public void SendMessage(string message) {
            _updateManager.AddChatMessage(message);
        }
    }
}