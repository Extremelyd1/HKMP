using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.Util;
using UnityEngine;

namespace HKMP.Game {
    public class GameManager {
        public GameManager() {
            ThreadUtil.Instantiate();

            var packetManager = new PacketManager();
            var networkManager = new NetworkManager(packetManager);
            var uiManager = new UI.UIManager(networkManager);
            
            uiManager.CreateUI();

            var clientManager = new ClientManager(networkManager, packetManager, uiManager);
            var serverManager = new ServerManager(networkManager, packetManager);
        }
    }
}