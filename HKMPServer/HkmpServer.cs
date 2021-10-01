using System;
using Hkmp;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using Version = Hkmp.Version;

namespace HkmpServer {
    internal class HkmpServer {
        public static void Main(string[] args) {
            Logger.SetLogger(new ConsoleLogger());

            var hkmpServer = new HkmpServer();

            if (args.Length == 1) {
                if (int.TryParse(args[0], out var port) && IsValidPort(port)) {
                    hkmpServer.Initialize(port);
                    return;
                }
            }

            hkmpServer.Initialize();
        }

        /**
         * Initialize the server with the given port, or ask for a port from the cli.
         */
        private void Initialize(int port = -1) {
            if (port == -1) {
                while (true) {
                    Console.Write("Enter a port: ");

                    var input = Console.ReadLine();

                    if (!int.TryParse(input, out port)) {
                        Console.WriteLine("Port not a valid integer");
                    } else {
                        if (!IsValidPort(port)) {
                            Console.WriteLine("Port should be between 0 and 65535");
                        } else {
                            break;
                        }
                    }
                }
            }

            var gameSettings = ConfigManager.LoadGameSettings(out var existed);
            if (!existed) {
                ConfigManager.SaveGameSettings(gameSettings);
            }

            StartServer(port, gameSettings);

            Console.ReadLine();
        }

        /**
         * Will start the server with the given port and game settings.
         */
        private void StartServer(int port, GameSettings gameSettings) {
            Logger.Get().Info(this, $"Starting server v{Version.String}");

            var packetManager = new PacketManager();

            var netServer = new NetServer(packetManager);

            var serverManager = new ServerManager(netServer, gameSettings, packetManager);

            new CommandManager(gameSettings, serverManager);

            serverManager.Start(port);
        }

        /**
         * Returns true if the given port is a valid port.
         */
        private static bool IsValidPort(int port) {
            return port >= 0 && port <= 65535;
        }
    }
}