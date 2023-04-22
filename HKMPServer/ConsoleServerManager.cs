using System;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using HkmpServer.Command;
using HkmpServer.Logging;

namespace HkmpServer {
    /// <summary>
    /// Specialization of the server manager for the console program.
    /// </summary>
    internal class ConsoleServerManager : ServerManager {
        /// <summary>
        /// The logger class for logging to console.
        /// </summary>
        private readonly ConsoleLogger _consoleLogger;
        
        public ConsoleServerManager(
            NetServer netServer,
            ServerSettings serverSettings,
            PacketManager packetManager,
            ConsoleLogger consoleLogger
        ) : base(netServer, serverSettings, packetManager) {
            _consoleLogger = consoleLogger;
            
            // Start loading addons
            AddonManager.LoadAddons();

            // Register a callback for when the application is closed to stop the server
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => {
                if (Environment.ExitCode == 5) {
                    return;
                }

                Stop();
            };
        }

        /// <inheritdoc />
        protected override void RegisterCommands() {
            base.RegisterCommands();

            CommandManager.RegisterCommand(new ExitCommand(this));
            CommandManager.RegisterCommand(new ConsoleSettingsCommand(this, InternalServerSettings));
            CommandManager.RegisterCommand(new LogCommand(_consoleLogger));
        }
    }
}
