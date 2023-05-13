using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hkmp.Game.Server.Auth;

/// <summary>
/// Authentication key list containing keys and IPs for banned users.
/// </summary>
internal class BanList : AuthKeyList {
    /// <summary>
    /// The file name of the ban list.
    /// </summary>
    private const string BanListFileName = "banned.json";

    /// <summary>
    /// Set of IP addresses of banned users.
    /// </summary>
    [JsonProperty("ip-addresses")] private readonly HashSet<string> _ipAddresses;

    protected BanList() {
        _ipAddresses = new HashSet<string>();
    }

    /// <summary>
    /// Whether a given IP address is banned.
    /// </summary>
    /// <param name="address">The address to check.</param>
    /// <returns>true if the address is banned; otherwise false</returns>
    public bool IsIpBanned(string address) {
        return _ipAddresses.Contains(address);
    }

    /// <summary>
    /// Add the given address to the ban list.
    /// </summary>
    /// <param name="address">The address to add.</param>
    public void AddIp(string address) {
        _ipAddresses.Add(address);

        WriteToFile();
    }

    /// <summary>
    /// Remove the given address from the ban list.
    /// </summary>
    /// <param name="address">The address to remove.</param>
    public void RemoveIp(string address) {
        _ipAddresses.Remove(address);

        WriteToFile();
    }

    /// <summary>
    /// Removes all addresses from the ban list.
    /// </summary>
    public void ClearIps() {
        _ipAddresses.Clear();

        WriteToFile();
    }

    /// <summary>
    /// Load the ban list from file.
    /// </summary>
    /// <returns>The loaded instance of the ban list or a new instance.</returns>
    public static BanList LoadFromFile() {
        return LoadFromFile(
            () => new BanList { FileName = BanListFileName },
            BanListFileName
        );
    }
}
