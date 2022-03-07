using System;
using System.Threading;
using Hkmp;
using Hkmp.Game.Server;

namespace HkmpServer.Command {
    public class ConsoleInputManager {
        private readonly ServerManager _serverManager;

        public ConsoleInputManager(ServerManager serverManager) {
            _serverManager = serverManager;
        }

        public void StartReading() {
            new Thread(() => {
                while (true) {
                    var consoleInput = Console.ReadLine();
                    if (consoleInput == null) {
                        continue;
                    }

                    consoleInput = "/" + consoleInput;

                    if (!_serverManager.TryProcessCommand(new ConsoleCommandSender(), consoleInput)) {
                        Logger.Get().Info(this, $"Unknown command: {consoleInput}");
                    }
                }
            }).Start();
        }
    }
}