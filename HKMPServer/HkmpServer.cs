using System;
using Hkmp;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using HkmpServer.Command;
using Version = Hkmp.Version;

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
            Logger.SetLogger(new ConsoleLogger());

            var port = args.Length == 1 ? GetCommandLinePort(args[0]) : GetCommandLinePort();

            var gameSettings = ConfigManager.LoadGameSettings(out var existed);
            if (!existed) {
                ConfigManager.SaveGameSettings(gameSettings);
            }

            StartServer(port, gameSettings);

            Console.ReadLine();
        }

        /// <summary>
        /// Will start the server with the given port and game settings.
        /// </summary>
        /// <param name="port">The port of the server.</param>
        /// <param name="gameSettings">The game settings for the server.</param>
        private void StartServer(int port, GameSettings gameSettings) {
            Console.WriteLine($"Starting server v{Version.String}");

            var packetManager = new PacketManager();

            var netServer = new NetServer(packetManager);

            var serverManager = new ConsoleServerManager(netServer, gameSettings, packetManager);
            serverManager.Initialize();

            new ConsoleInputManager(serverManager).StartReading();

            serverManager.Start(port);
        }

        /// <summary>
        /// Get a port from the given command line input and keep asking until a valid port is given.
        /// </summary>
        /// <param name="input">The command line input as a string.</param>
        /// <returns>An integer representing a valid port.</returns>
        private int GetCommandLinePort(string input = "") {
            while (true) {
                if (!string.IsNullOrEmpty(input)) {
                    if (!ParsePort(input, out var port)) {
                        Console.WriteLine("Port is not valid, should be an integer between 0 and 65535");
                    } else {
                        return port;
                    }
                }

                Console.Write("Enter a port: ");

                input = Console.ReadLine();
            }
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