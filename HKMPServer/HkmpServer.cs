using System.Threading.Tasks;
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
            Logger.AddLogger(new ConsoleLogger(consoleInputManager));
            Logger.AddLogger(new RollingFileLogger());

            if (args.Length < 1) {
                Logger.Info("Please provide a port in the arguments");
                return;
            }

            if (string.IsNullOrEmpty(args[0]) || !ParsePort(args[0], out var port)) {
                Logger.Info("Invalid port, should be an integer between 0 and 65535");
                return;
            }

            var gameSettings = ConfigManager.LoadGameSettings(out var existed);
            if (!existed) {
                ConfigManager.SaveGameSettings(gameSettings);
            }

            StartServer(port, gameSettings, consoleInputManager);
        }

        /// <summary>
        /// Will start the server with the given port and game settings.
        /// </summary>
        /// <param name="port">The port of the server.</param>
        /// <param name="gameSettings">The game settings for the server.</param>
        /// <param name="consoleInputManager">The input manager for command-line input.</param>
        private void StartServer(
            int port,
            GameSettings gameSettings,
            ConsoleInputManager consoleInputManager
        ) {
            Logger.Info($"Starting server v{Version.String}");

            var packetManager = new PacketManager();

            var netServer = new NetServer(packetManager);

            var serverManager = new ConsoleServerManager(netServer, gameSettings, packetManager);
            serverManager.Initialize();
            serverManager.Start(port);

            // TODO: make an event in ServerManager that we can register for so we know when the server shuts down
            consoleInputManager.ConsoleInputEvent += input => {
                Logger.Info(input);
                if (!serverManager.TryProcessCommand(new ConsoleCommandSender(), "/" + input)) {
                    Logger.Info($"Unknown command: {input}");
                }
            };
            consoleInputManager.Start();
        }

        /// <summary>
        /// Try to parse the given input as a networking port.
        /// </summary>
        /// <param name="input">The string to parse.</param>
        /// <param name="port">Will be set to the parsed port if this method returns true, or 0 if the method
        /// returns false.</param>
        /// <returns>True if the given input was parsed as a valid port, false otherwise.</returns>
        private static bool ParsePort(string input, out int port) {
            if (!int.TryParse(input, out port)) {
                return false;
            }

            if (!IsValidPort(port)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the given port is a valid networking port.
        /// </summary>
        /// <param name="port">The port to check.</param>
        /// <returns>True if the port is valid, false otherwise.</returns>
        private static bool IsValidPort(int port) {
            return port >= 0 && port <= 65535;
        }
    }
}
