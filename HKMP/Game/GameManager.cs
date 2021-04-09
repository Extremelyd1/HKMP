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


            ThreadUtil.Instantiate();
            
            FontManager.LoadFonts();
            TextureManager.LoadTextures();

            var packetManager = new PacketManager();
            var serverKnightsManager = new ServerKnightsManager();

            var networkManager = new NetworkManager(packetManager,serverKnightsManager);
            serverKnightsManager._networkManager = networkManager;

            var clientGameSettings = new Settings.GameSettings();
            var serverGameSettings = modSettings.GameSettings ?? new Settings.GameSettings();

            var playerManager = new PlayerManager(networkManager, clientGameSettings, modSettings,serverKnightsManager);

            var animationManager =
                new AnimationManager(networkManager, playerManager, packetManager, clientGameSettings,serverKnightsManager);

            var mapManager = new MapManager(networkManager, clientGameSettings);

            var clientManager = new ClientManager(
                networkManager,
                playerManager,
                animationManager,
                mapManager,
                clientGameSettings,
                packetManager,
                serverKnightsManager
            );


            var serverManager = new ServerManager(networkManager, serverGameSettings, packetManager , serverKnightsManager);

            new UI.UIManager(
                serverManager, 
                clientManager, 
                clientGameSettings,
                serverGameSettings, 
                modSettings
            );

            serverKnightsManager.skinManager.preloadSkinSources();

        }
    }
}