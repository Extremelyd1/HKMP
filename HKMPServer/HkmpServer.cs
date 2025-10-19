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

            var hasPortArg = false;
            ushort port = 0;

            if (args.Length > 0) {
                if (string.IsNullOrEmpty(args[0]) || !ushort.TryParse(args[0], out port)) {
                    Logger.Info("Invalid port, should be an integer between 0 and 65535");
                    return;
                }

                hasPortArg = true;
            }

            if (!ConfigManager.LoadServerSettings(out var serverSettings)) {
                ConfigManager.SaveServerSettings(serverSettings);
            }

            // Load the console settings and note whether they existed or not
            var consoleSettingsExisted = ConfigManager.LoadConsoleSettings(out var consoleSettings);
            // If the user supplied a port on the arguments to the program, we override the loaded settings with
            // the port
            if (hasPortArg) {
                consoleSettings.Port = port;
            }

            // If the settings did not yet exist, we now save the settings possibly with the argument provided port
            if (!consoleSettingsExisted) {
                ConfigManager.SaveConsoleSettings(consoleSettings);
            }

            StartServer(consoleSettings, serverSettings, consoleInputManager, consoleLogger);
        }

        /// <summary>
        /// Will start the server with the given port and server settings.
        /// </summary>
        /// <param name="consoleSettings">The console settings for the program.</param>
        /// <param name="serverSettings">The server settings for the server.</param>
        /// <param name="consoleInputManager">The input manager for command-line input.</param>
        /// <param name="consoleLogger">The logging class for logging to console.</param>
        private void StartServer(
            ConsoleSettings consoleSettings,
            ServerSettings serverSettings,
            ConsoleInputManager consoleInputManager,
            ConsoleLogger consoleLogger
        ) {
            Logger.Info($"Starting server v{Version.String}");

            var packetManager = new PacketManager();

            var netServer = new NetServer(packetManager);

            var serverManager = new ConsoleServerManager(netServer, packetManager, serverSettings, consoleLogger);
            serverManager.Initialize();
            serverManager.Start(consoleSettings.Port, consoleSettings.FullSynchronisation);

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
