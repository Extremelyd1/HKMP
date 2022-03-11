using System;
using System.Threading;
using Hkmp;
using Hkmp.Game.Server;

namespace HkmpServer.Command {
    /// <summary>
    /// Input manager for console command-line input.
    /// </summary>
    internal class ConsoleInputManager {
        /// <summary>
        /// The server manager instance.
        /// </summary>
        private readonly ServerManager _serverManager;

        /// <summary>
        /// Construct the input manager with the given server manager.
        /// </summary>
        /// <param name="serverManager">The server manager instance.</param>
        public ConsoleInputManager(ServerManager serverManager) {
            _serverManager = serverManager;
        }

        /// <summary>
        /// Starts reading command-line input.
        /// </summary>
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