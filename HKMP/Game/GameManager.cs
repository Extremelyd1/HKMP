using Hkmp.Animation;
using Hkmp.Game.Client;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking;
using Hkmp.Networking.Packet;
using Hkmp.Ui;
using Hkmp.Ui.Resources;
using Hkmp.Util;

namespace Hkmp.Game {
    /**
     * Instantiates all necessary classes to start multiplayer activities
     */
    public class GameManager {

        private readonly NetworkManager _networkManager;
        
        public GameManager(ModSettings modSettings) {
            ThreadUtil.Instantiate();

            FontManager.LoadFonts();
            TextureManager.LoadTextures();

            var packetManager = new PacketManager();

            _networkManager = new NetworkManager(packetManager);

            var clientGameSettings = new Settings.GameSettings();
            var serverGameSettings = modSettings.GameSettings ?? new Settings.GameSettings();

            var playerManager = new PlayerManager(packetManager, clientGameSettings);

            var animationManager =
                new AnimationManager(_networkManager, playerManager, packetManager, clientGameSettings);

            var mapManager = new MapManager(_networkManager, clientGameSettings);

            var clientManager = new ClientManager(
                _networkManager,
                playerManager,
                animationManager,
                mapManager,
                clientGameSettings,
                packetManager
            );

            var serverManager = new ServerManager(_networkManager.GetInternalNetServer(), serverGameSettings, packetManager);

            new UiManager(
                serverManager,
                clientManager,
                clientGameSettings,
                serverGameSettings,
                modSettings,
                _networkManager.GetInternalNetClient()
            );
        }
    }
}