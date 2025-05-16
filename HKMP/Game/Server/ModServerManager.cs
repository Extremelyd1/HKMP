using Hkmp.Game.Client.Save;
using Hkmp.Game.Command.Server;
using Hkmp.Game.Server.Save;
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
    /// The UiManager instance for registering events for starting and stopping a server.
    /// </summary>
    private readonly UiManager _uiManager;
    
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
        UiManager uiManager,
        ModSettings modSettings
    ) : base(netServer, serverSettings) {
        _uiManager = uiManager;
        _modSettings = modSettings;
    }

    /// <inheritdoc />
    public override void Initialize(PacketManager packetManager) {
        base.Initialize(packetManager);
        
        // Start addon loading once all mods have finished loading
        ModHooks.FinishedLoadingModsHook += AddonManager.LoadAddons;

        // Register handlers for UI events
        _uiManager.RequestServerStartHostEvent += OnRequestServerStartHost;
        _uiManager.RequestServerStopHostEvent += Stop;

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

    /// <summary>
    /// Callback for when a local save is loaded.
    /// </summary>
    /// <param name="modSaveFile">The deserialized ModSaveFile instance.</param>
    public void OnLoadLocal(ModSaveFile modSaveFile) {
        _loadedLocalSaveData = modSaveFile.ToServerSaveData();
    }

    /// <summary>
    /// Callback for when a local save is saved.
    /// </summary>
    /// <returns>The ModSaveFile instance to serialize to file.</returns>
    public ModSaveFile OnSaveLocal() {
        return ModSaveFile.FromServerSaveData(ServerSaveData);
    }
}
