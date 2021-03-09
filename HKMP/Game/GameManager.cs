using HKMP.Animation;
using HKMP.Game.Server;
using HKMP.Game.Settings;
using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.Util;
using GameSettings = HKMP.Game.Settings.GameSettings;

namespace HKMP.Game {
    /**
     * Instantiates all necessary classes to start multiplayer activities
     */
    public class GameManager {
        public GameManager(ModSettings modSettings) {
            ThreadUtil.Instantiate();

            var packetManager = new PacketManager();
            var networkManager = new NetworkManager(packetManager);

            var clientGameSettings = new Settings.GameSettings();
            var serverGameSettings = modSettings.GameSettings ?? new Settings.GameSettings();

            var playerManager = new PlayerManager(networkManager, clientGameSettings, modSettings);

            var animationManager =
                new AnimationManager(networkManager, playerManager, packetManager, clientGameSettings);

            var mapManager = new MapManager(networkManager, clientGameSettings, packetManager);

            var clientManager = new ClientManager(
                networkManager,
                playerManager,
                animationManager,
                clientGameSettings,
                packetManager
            );
            var serverManager = new ServerManager(networkManager, serverGameSettings, packetManager);

            var uiManager = new UI.UIManager(serverManager, clientManager, serverGameSettings, modSettings);
            uiManager.CreateUI();
        }
    }
}