using Hkmp.Game.Client.Save;
using Hkmp.Game.Command.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;
using Hkmp.Ui;
using Modding;

namespace Hkmp.Game.Server;

/// <summary>
/// Specialization of <see cref="ServerManager"/> that adds handlers for the mod specific things.
/// </summary>
internal class ModServerManager : ServerManager {
    /// <summary>
    /// Save data that was loaded from selecting a save file. Will be retroactively applied to a server, if one was
    /// requested to be started after selecting a save file.
    /// </summary>
    private ServerSaveData _loadedLocalSaveData;
    
    public ModServerManager(
        NetServer netServer,
        ServerSettings serverSettings,
        PacketManager packetManager,
        UiManager uiManager
    ) : base(netServer, serverSettings, packetManager) {
        // Start addon loading once all mods have finished loading
        ModHooks.FinishedLoadingModsHook += AddonManager.LoadAddons;

        // Register handlers for UI events
        uiManager.RequestServerStartHostEvent += OnRequestServerStartHost;
        uiManager.RequestServerStopHostEvent += Stop;

        // Register application quit handler
        ModHooks.ApplicationQuitHook += Stop;
    }

    private void OnRequestServerStartHost(int port) {
        // Get the global save data from the save manager, which obtains the global save data from the loaded
        // save file that the user selected. Then we import the player save data from the (potentially) loaded
        // modded save file from the user selected save file.
        ServerSaveData = new ServerSaveData {
            GlobalSaveData = SaveManager.GetCurrentGlobalSaveData()
        };

        if (_loadedLocalSaveData != null) {
            ServerSaveData.PlayerSaveData = _loadedLocalSaveData.PlayerSaveData;
        }
            
        Start(port);
    }

    /// <inheritdoc />
    protected override void RegisterCommands() {
        base.RegisterCommands();

        CommandManager.RegisterCommand(new SettingsCommand(this, InternalServerSettings));
    }

    public void OnLoadLocal(ServerSaveData serverSaveData) {
        _loadedLocalSaveData = serverSaveData;
    }

    public ServerSaveData OnSaveLocal() {
        return ServerSaveData;
    }
}
