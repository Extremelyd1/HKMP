using System;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using HkmpServer.Command;

namespace HkmpServer {
    public class ConsoleServerManager : ServerManager {
        public ConsoleServerManager(
            NetServer netServer, 
            GameSettings gameSettings, 
            PacketManager packetManager
        ) : base(netServer, gameSettings, packetManager) {
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => Stop();
        }

        protected override void RegisterCommands() {
            base.RegisterCommands();

            CommandManager.RegisterCommand(new ExitCommand(this));
        }
    }
}