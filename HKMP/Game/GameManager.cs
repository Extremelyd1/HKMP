using HKMP.Animation;
using HKMP.Game.Client;
using HKMP.ServerKnights;
using HKMP.Game.Server;
using HKMP.Game.Settings;
using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.UI.Resources;
using HKMP.Util;

namespace HKMP.Game {
    /**
     * Instantiates all necessary classes to start multiplayer activities
     */
    public class GameManager {
        public GameManager(ModSettings modSettings) {

            var skinManager = new SkinManager();

            ThreadUtil.Instantiate();
            
            FontManager.LoadFonts();
            TextureManager.LoadTextures();

            var packetManager = new PacketManager();
            var networkManager = new NetworkManager(packetManager,skinManager);

            var clientGameSettings = new Settings.GameSettings();
            var serverGameSettings = modSettings.GameSettings ?? new Settings.GameSettings();

            var playerManager = new PlayerManager(networkManager, clientGameSettings, modSettings,skinManager);

            var animationManager =
                new AnimationManager(networkManager, playerManager, packetManager, clientGameSettings,skinManager);

            var mapManager = new MapManager(networkManager, clientGameSettings);

            var clientManager = new ClientManager(
                networkManager,
                playerManager,
                animationManager,
                mapManager,
                clientGameSettings,
                packetManager,
                skinManager
            );
            var serverManager = new ServerManager(networkManager, serverGameSettings, packetManager , skinManager);

            new UI.UIManager(
                serverManager, 
                clientManager, 
                clientGameSettings,
                serverGameSettings, 
                modSettings
            );
            skinManager.preloadSkinSources();

        }
    }
}