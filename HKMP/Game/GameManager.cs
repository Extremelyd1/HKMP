using HKMP.Networking;
using HKMP.Networking.Packet;

namespace HKMP.Game {
    public class GameManager {
        public GameManager() {
            var packetManager = new PacketManager();
            var networkManager = new NetworkManager(packetManager);
            new UI.UIManager(networkManager).CreateUI();
        }
    }
}