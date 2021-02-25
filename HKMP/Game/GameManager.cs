using HKMP.Animation;
using HKMP.Game.Server;
using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.Util;

namespace HKMP.Game {
    /**
     * Instantiates all necessary classes to start multiplayer activities
     */
    public class GameManager {
        public GameManager() {
            ThreadUtil.Instantiate();

            var packetManager = new PacketManager();
            var networkManager = new NetworkManager(packetManager);
            var uiManager = new UI.UIManager(networkManager);
            var playerManager = new PlayerManager();
            
            var animationManager = new AnimationManager(networkManager, playerManager, packetManager);

            var clientManager = new ClientManager(networkManager, uiManager, playerManager, animationManager, packetManager);
            var serverManager = new ServerManager(networkManager, packetManager);
            
            uiManager.CreateUI();
        }
    }
}