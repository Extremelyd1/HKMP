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
    /// The mod settings instance for retrieving the auth key of the local player to set player save data when
    /// hosting a server.
    /// </summary>
    private readonly ModSettings _modSettings;
    
    /// <summary>
    /// Save data that was loaded from selecting a save file. Will be retroactively applied to a server, if one was
    /// requested to be started after selecting a save file.
    /// </summary>
    private ServerSaveData _loadedLocalSaveData;
    
    public ModServerManager(
        NetServer netServer,
        ServerSettings serverSettings,
        PacketManager packetManager,
        UiManager uiManager,
        ModSettings modSettings
    ) : base(netServer, serverSettings, packetManager) {
        _modSettings = modSettings;
        
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
        // save file that the user selected
        ServerSaveData.GlobalSaveData = SaveManager.GetCurrentSaveData(true);

        // Then we import the player save data from the (potentially) loaded modded save file from the user selected
        // save file
        if (_loadedLocalSaveData != null) {
            ServerSaveData.PlayerSaveData = _loadedLocalSaveData.PlayerSaveData;
        }
        
        // Lastly, we get the player save data from the save manager, which obtains the player save data from the
        // loaded save file that the user selected. We add this data to the server save as the local player
        ServerSaveData.PlayerSaveData[_modSettings.AuthKey] = SaveManager.GetCurrentSaveData(false);
            
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
