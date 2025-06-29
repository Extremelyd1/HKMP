using System.Collections.Generic;
using Hkmp.Menu;
using Modding.Converters;
using Newtonsoft.Json;

namespace Hkmp.Game.Settings;

/// <summary>
/// Settings class that stores user preferences.
/// </summary>
internal class ModSettings {
    /// <summary>
    /// The authentication key for the user.
    /// </summary>
    public string AuthKey { get; set; }

    /// <summary>
    /// The keybinds for HKMP.
    /// </summary>
    [JsonConverter(typeof(PlayerActionSetConverter))]
    public Keybinds Keybinds { get; set; } = new();

    /// <summary>
    /// The last used address to join a server.
    /// </summary>
    public string ConnectAddress { get; set; }

    /// <summary>
    /// The last used port to join a server.
    /// </summary>
    public int ConnectPort { get; set; } = -1;

    /// <summary>
    /// The last used username to join a server.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Whether to display a UI element for the ping.
    /// </summary>
    public bool DisplayPing { get; set; }

    /// <summary>
    /// Set of addon names for addons that are disabled by the user.
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public HashSet<string> DisabledAddons { get; set; } = [];

    /// <summary>
    /// Whether full synchronisation of bosses, enemies, worlds, and saves is enabled.
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public bool FullSynchronisation { get; set; } = true;

    /// <summary>
    /// The last used server settings in a hosted server.
    /// </summary>
    public ServerSettings ServerSettings { get; set; }
}
