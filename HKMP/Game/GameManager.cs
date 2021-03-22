using HKMP.Animation;
using HKMP.Game.Client;
using HKMP.Game.Server;
using HKMP.Game.Settings;
using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.UI.Resources;
using HKMP.Util;
using GameSettings = HKMP.Game.Settings.GameSettings;

namespace HKMP.Game {
    /**
     * Instantiates all necessary classes to start multiplayer activities
     */
    public class GameManager {
        public GameManager(ModSettings modSettings) {
            ThreadUtil.Instantiate();
            
            FontManager.LoadFonts();
            TextureManager.LoadTextures();

            var packetManager = new PacketManager();
            var networkManager = new NetworkManager(packetManager);

            var clientGameSettings = new Settings.GameSettings();
            var serverGameSettings = modSettings.GameSettings ?? new Settings.GameSettings();

            var playerManager = new PlayerManager(networkManager, clientGameSettings, modSettings);

            var animationManager =
                new AnimationManager(networkManager, playerManager, packetManager, clientGameSettings);
            
            new DreamShieldManager(networkManager, playerManager, packetManager);

            var mapManager = new MapManager(networkManager, clientGameSettings, packetManager);

            var clientManager = new ClientManager(
                networkManager,
                playerManager,
                animationManager,
                mapManager,
                clientGameSettings,
                packetManager
            );
            var serverManager = new ServerManager(networkManager, serverGameSettings, packetManager);

            new UI.UIManager(serverManager, clientManager, serverGameSettings, modSettings);
        }
    }
}