using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;

namespace HKMP.Game {
    /**
     * Settings related to gameplay that is shared between server and clients
     */
    public class GameSettings {
        // TODO: get rid of public static instances, and figure out a way how to do animation effects without these
        public static GameSettings ServerInstance;
        public static GameSettings ClientInstance;

        public bool IsPvpEnabled { get; set; }

        // Constructor for the server, which is only to read and write to this class
        public GameSettings() {
            ServerInstance = this;
        }

        // Constructor for clients, which will register a callback for when the settings change
        public GameSettings(PacketManager packetManager) {
            packetManager.RegisterClientPacketHandler<GameSettingsUpdatePacket>(PacketId.GameSettingsUpdated, OnGameSettingsUpdated);

            ClientInstance = this;
        }

        private void OnGameSettingsUpdated(GameSettingsUpdatePacket packet) {
            // Check whether the PvP state changed
            if (IsPvpEnabled != packet.IsPvpEnabled) {
                IsPvpEnabled = packet.IsPvpEnabled;
                
                Logger.Info(this, $"PvP is now {(packet.IsPvpEnabled ? "Enabled" : "Disabled")}");
            }
        }
    }
}