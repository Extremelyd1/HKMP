using System;
using Hkmp;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using HkmpServer.Command;
using Version = Hkmp.Version;

namespace HkmpServer {
    internal class HkmpServer {
        /// <summary>
        /// Initialize the server with the given port, or ask for a port from the command line.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        public void Initialize(string[] args) {
            Logger.SetLogger(new ConsoleLogger());

            var port = 0;
            
            if (args.Length == 1) {
                port = GetCommandLinePort(args[0]);
            }

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
        private int GetCommandLinePort(string input) {
            while (true) {
                if (!int.TryParse(input, out var port)) {
                    Console.WriteLine("Port not a valid integer");
                } else {
                    if (!IsValidPort(port)) {
                        Console.WriteLine("Port should be between 0 and 65535");
                    } else {
                        return port;
                    }
                }
                
                Console.Write("Enter a port: ");

                input = Console.ReadLine();
            }
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