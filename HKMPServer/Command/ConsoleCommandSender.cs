using Hkmp;
using Hkmp.Api.Command.Server;

namespace HkmpServer.Command {
    /// <summary>
    /// Specialization of a command sender that is the console.
    /// </summary>
    internal class ConsoleCommandSender : ICommandSender {
        /// <inheritdoc />
        public bool IsAuthorized => true;
        /// <inheritdoc />
        public CommandSenderType Type => CommandSenderType.Console;

        /// <inheritdoc />
        public void SendMessage(string message) {
            Logger.Get().Info(this, message);
        }
    }
}