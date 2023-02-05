using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hkmp.Game.Server.Auth;

/// <summary>
/// Authentication key list containing keys for white-listed users.
/// </summary>
internal class WhiteList : AuthKeyList {
    /// <summary>
    /// The file name of the white-list.
    /// </summary>
    private const string WhiteListFileName = "whitelist.json";

    /// <summary>
    /// Whether the white-list is enabled.
    /// </summary>
    [JsonProperty("enabled")] private bool _isEnabled;

    /// <inheritdoc cref="_isEnabled" />
    [JsonIgnore]
    public bool IsEnabled {
        get => _isEnabled;
        set {
            _isEnabled = value;
            WriteToFile();
        }
    }

    /// <summary>
    /// Set of names of users that are pre-listed, meaning that the auth key will be
    /// white-listed as soon as a player with that name logs in.
    /// </summary>
    [JsonProperty("pre-listed")] private readonly HashSet<string> _preListed;

    public WhiteList() {
        _preListed = new HashSet<string>();
    }

    /// <summary>
    /// Whether a given name is pre-listed.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns>true if the name is pre-listed, false otherwise.</returns>
    public bool IsPreListed(string name) {
        return _preListed.Contains(name.ToLower());
    }

    /// <summary>
    /// Add the given name to the pre-list.
    /// </summary>
    /// <param name="name">The name to add.</param>
    public void AddPreList(string name) {
        _preListed.Add(name.ToLower());

        WriteToFile();
    }

    /// <summary>
    /// Remove the given name from the pre-list.
    /// </summary>
    /// <param name="name">The name to remove.</param>
    public void RemovePreList(string name) {
        _preListed.Remove(name.ToLower());

        WriteToFile();
    }

    /// <summary>
    /// Removes all names from the pre-list.
    /// </summary>
    public void ClearPreList() {
        _preListed.Clear();

        WriteToFile();
    }

    /// <summary>
    /// Get a string containing comma separated names of all pre-listed users.
    /// </summary>
    /// <returns>A string containing the pre-listed usernames.</returns>
    public string GetPreListed() {
        return string.Join(", ", _preListed);
    }

    /// <summary>
    /// Load the white-list from file.
    /// </summary>
    /// <returns>The loaded instance of the white-list or a new instance.</returns>
    public static WhiteList LoadFromFile() {
        return LoadFromFile(
            () => new WhiteList { FileName = WhiteListFileName },
            WhiteListFileName
        );
    }
}
