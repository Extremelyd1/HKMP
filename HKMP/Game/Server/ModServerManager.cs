using Hkmp.Game.Command.Server;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using Hkmp.Ui;
using Modding;

namespace Hkmp.Game.Server {
    public class ModServerManager : ServerManager {
        public ModServerManager(
            NetServer netServer, 
            Settings.GameSettings gameSettings, 
            PacketManager packetManager,
            UiManager uiManager
        ) : base(netServer, gameSettings, packetManager) {
            // Register handlers for UI events
            uiManager.ConnectInterface.StartHostButtonPressed += Start;
            uiManager.ConnectInterface.StopHostButtonPressed += Stop;

            // Register application quit handler
            ModHooks.ApplicationQuitHook += Stop;
        }

        protected override void RegisterCommands() {
            base.RegisterCommands();
            
            CommandManager.RegisterCommand(new SettingsCommand(this, GameSettings));
        }
    }
}