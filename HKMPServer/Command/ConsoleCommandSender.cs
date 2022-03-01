using Hkmp;
using Hkmp.Api.Command.Server;

namespace HkmpServer.Command {
    public class ConsoleCommandSender : ICommandSender {
        public bool IsAuthorized => true;

        public void SendMessage(string message) {
            Logger.Get().Info(this, message);
        }
    }
}