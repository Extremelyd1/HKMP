using Hkmp;
using Hkmp.Game.Settings;
using Hkmp.Logging;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using HkmpServer.Command;
using HkmpServer.Logging;

namespace HkmpServer {
    /// <summary>
    /// The HKMP Server class.
    /// </summary>
    internal class HkmpServer {
        /// <summary>
        /// Initialize the server with the given port, or ask for a port from the command line.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public void Initialize(string[] args) {
            var consoleInputManager = new ConsoleInputManager();
            var consoleLogger = new ConsoleLogger(consoleInputManager);
            Logger.AddLogger(consoleLogger);
            Logger.AddLogger(new RollingFileLogger());

            if (args.Length < 1) {
                Logger.Info("Please provide a port in the arguments");
                return;
            }

            var portArg = args[0];

            if (string.IsNullOrEmpty(portArg) || !ushort.TryParse(portArg, out var port)) {
                Logger.Info("Invalid port, should be an integer between 0 and 65535");
                return;
            }

            var serverSettings = ConfigManager.LoadServerSettings(out var existed);
            if (!existed) {
                ConfigManager.SaveServerSettings(serverSettings);
            }

            StartServer(port, serverSettings, consoleInputManager, consoleLogger);
        }

        /// <summary>
        /// Will start the server with the given port and server settings.
        /// </summary>
        /// <param name="port">The port of the server.</param>
        /// <param name="serverSettings">The server settings for the server.</param>
        /// <param name="consoleInputManager">The input manager for command-line input.</param>
        /// <param name="consoleLogger">The logging class for logging to console.</param>
        private void StartServer(
            ushort port,
            ServerSettings serverSettings,
            ConsoleInputManager consoleInputManager,
            ConsoleLogger consoleLogger
        ) {
            Logger.Info($"Starting server v{Version.String}");

            var packetManager = new PacketManager();

            var netServer = new NetServer(packetManager);

            var serverManager = new ConsoleServerManager(netServer, serverSettings, packetManager, consoleLogger);
            serverManager.Initialize();
            serverManager.Start((int)port);

            // TODO: make an event in ServerManager that we can register for so we know when the server shuts down
            consoleInputManager.ConsoleInputEvent += input => {
                Logger.Info(input);
                if (!serverManager.TryProcessCommand(new ConsoleCommandSender(), "/" + input)) {
                    Logger.Info($"Unknown command: {input}");
                }
            };
            consoleInputManager.Start();
        }
    }
}
