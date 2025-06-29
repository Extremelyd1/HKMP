using System.Collections.Generic;
using Hkmp.Game.Server.Save;
using Hkmp.Game.Settings;
using Hkmp.Logging;
using Hkmp.Menu;
using Hkmp.Util;
using Modding;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp;

/// <summary>
/// Mod class for the HKMP mod.
/// </summary>
internal class HkmpMod : Mod, IGlobalSettings<ModSettings>, ILocalSettings<ModSaveFile>, ICustomMenuMod {
    /// <summary>
    /// Dictionary containing preloaded objects by scene name and object path.
    /// </summary>
    public static Dictionary<string, Dictionary<string, GameObject>> PreloadedObjects;
    
    /// <summary>
    /// Statically create Settings object, so it can be accessed early.
    /// </summary>
    private ModSettings _modSettings = new ModSettings();

    /// <summary>
    /// The game manager instance.
    /// </summary>
    private Game.GameManager _gameManager;

    /// <summary>
    /// Construct the HKMP mod.
    /// </summary>
    public HkmpMod() : base("HKMP") {
    }

    /// <inheritdoc />
    public override string GetVersion() {
        return Version.String;
    }

    /// <inheritdoc />
    public override List<(string, string)> GetPreloadNames() {
        return [
            ("GG_Sly", "Battle Scene/Sly Boss/Cyclone Tink"),
            ("GG_Sly", "Battle Scene/Sly Boss/S1")
        ];
    }

    /// <inheritdoc />
    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects) {
        PreloadedObjects = preloadedObjects;
        
        // Add the logger that logs to the ModLog
        Logger.AddLogger(new ModLogger());

        Logger.Info($"Initializing HKMP v{Version.String}");

        // Create a persistent gameObject where we can add the MonoBehaviourUtil to
        var gameObject = new GameObject("HKMP Persistent GameObject");
        Object.DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<MonoBehaviourUtil>();

        _gameManager = new Game.GameManager(_modSettings);
    }

    /// <inheritdoc />
    public void OnLoadGlobal(ModSettings modSettings) {
        _modSettings = modSettings ?? new ModSettings();
    }

    /// <inheritdoc />
    public ModSettings OnSaveGlobal() {
        return _modSettings;
    }

    /// <inheritdoc />
    public void OnLoadLocal(ModSaveFile modSaveFile) {
        _gameManager?.ServerManager?.OnLoadLocal(modSaveFile);
    }
    
    /// <inheritdoc />
    public ModSaveFile OnSaveLocal() {
        return _gameManager.ServerManager.OnSaveLocal();
    }

    /// <inheritdoc />
    public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates) {
        return ModMenu.CreateMenu(
            modListMenu, 
            _modSettings, 
            _gameManager.ClientManager, 
            _gameManager.ServerManager, 
            _gameManager.NetClient
        );
    }

    /// <inheritdoc />
    public bool ToggleButtonInsideMenu => true;
}
