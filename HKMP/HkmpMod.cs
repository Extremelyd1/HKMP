using System.Collections.Generic;
using Hkmp.Game.Settings;
using Hkmp.Logging;
using Hkmp.Util;
using Modding;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp;

/// <summary>
/// Mod class for the HKMP mod.
/// </summary>
internal class HkmpMod : Mod, IGlobalSettings<ModSettings> {
    /// <summary>
    /// Dictionary containing preloaded objects by scene name and object path.
    /// </summary>
    public static Dictionary<string, Dictionary<string, GameObject>> PreloadedObjects;
    
    /// <summary>
    /// Statically create Settings object, so it can be accessed early.
    /// </summary>
    private ModSettings _modSettings = new ModSettings();

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
        return new List<(string, string)> {
            ("GG_Sly", "Battle Scene/Sly Boss/Cyclone Tink"),
            ("GG_Sly", "Battle Scene/Sly Boss/S1")
        };
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

        var gameManager = new Game.GameManager(_modSettings);
    }

    /// <inheritdoc />
    public void OnLoadGlobal(ModSettings modSettings) {
        _modSettings = modSettings ?? new ModSettings();
    }

    /// <inheritdoc />
    public ModSettings OnSaveGlobal() {
        return _modSettings;
    }
}
