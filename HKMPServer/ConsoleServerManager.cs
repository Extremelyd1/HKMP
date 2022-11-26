using System;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using HkmpServer.Command;

namespace HkmpServer {
    /// <summary>
    /// Specialization of the server manager for the console program.
    /// </summary>
    internal class ConsoleServerManager : ServerManager {
        public ConsoleServerManager(
            NetServer netServer,
            GameSettings gameSettings,
            PacketManager packetManager
        ) : base(netServer, gameSettings, packetManager) {
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
            CommandManager.RegisterCommand(new ConsoleSettingsCommand(this, GameSettings));
        }
    }
}
