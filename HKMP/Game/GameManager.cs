using Hkmp.Game.Client;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using Hkmp.Ui;
using Hkmp.Ui.Resources;
using Hkmp.Util;

namespace Hkmp.Game;

/// <summary>
/// Instantiates all necessary classes to start multiplayer activities.
/// </summary>
internal class GameManager {
    /// <summary>
    /// The server manager instance for the mod.
    /// </summary>
    public readonly ModServerManager ServerManager;
    
    /// <summary>
    /// Constructs this GameManager instance by instantiating all other necessary classes.
    /// </summary>
    /// <param name="modSettings">The loaded ModSettings instance or null if no such instance could be
    /// loaded.</param>
    public GameManager(ModSettings modSettings) {
        ThreadUtil.Instantiate();

        FontManager.LoadFonts();
        TextureManager.LoadTextures();

        var packetManager = new PacketManager();

        var netClient = new NetClient(packetManager);
        var netServer = new NetServer(packetManager);

        var clientServerSettings = new ServerSettings();
        if (modSettings.ServerSettings == null) {
            modSettings.ServerSettings = new ServerSettings();
        }
        var serverServerSettings = modSettings.ServerSettings;

        var uiManager = new UiManager(
            modSettings,
            netClient
        );

        ServerManager = new ModServerManager(
            netServer,
            serverServerSettings,
            uiManager,
            modSettings
        );
        ServerManager.Initialize(packetManager);

        new ClientManager(
            netClient,
            packetManager,
            uiManager,
            clientServerSettings,
            modSettings
        ).Initialize(ServerManager, packetManager);
    }
}
