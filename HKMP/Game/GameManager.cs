using Hkmp.Animation;
using Hkmp.Game.Client;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking;
using Hkmp.Networking.Packet;
using Hkmp.Ui.Resources;
using Hkmp.Util;

namespace Hkmp.Game {
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

            var clientGameSettings = new Game.Settings.GameSettings();
            var serverGameSettings = modSettings.GameSettings ?? new Game.Settings.GameSettings();

            var playerManager = new PlayerManager(packetManager, clientGameSettings);

            var animationManager =
                new AnimationManager(networkManager, playerManager, packetManager, clientGameSettings);

            var mapManager = new MapManager(networkManager, clientGameSettings);

            var clientManager = new ClientManager(
                networkManager,
                playerManager,
                animationManager,
                mapManager,
                clientGameSettings,
                packetManager
            );

            var serverManager = new ServerManager(networkManager.GetNetServer(), serverGameSettings, packetManager);

            new Ui.UiManager(
                serverManager,
                clientManager,
                clientGameSettings,
                serverGameSettings,
                modSettings,
                networkManager.GetNetClient()
            );
        }
    }
}