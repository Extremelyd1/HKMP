using System;
using Hkmp.Api.Command.Server;
using Hkmp.Game.Server;

namespace HkmpServer.Command {
    public class ExitCommand : IServerCommand {
        public string Trigger => "/exit";
        public string[] Aliases => Array.Empty<string>();
        public bool AuthorizedOnly => true;

        private readonly ServerManager _serverManager;

        public ExitCommand(ServerManager serverManager) {
            _serverManager = serverManager;
        }

        public void Execute(ICommandSender commandSender, string[] arguments) {
            if (commandSender.Type == CommandSenderType.Console) {
                _serverManager.Stop();

                commandSender.SendMessage("Exiting server...");
                Environment.Exit(0);

                return;
            }
            
            commandSender.SendMessage("This command can only be execute as the console");
        }
    }
}