using System;
using System.Collections.Generic;
using Hkmp.Api.Client.Networking;
using Hkmp.Game.Settings;
using Hkmp.Logging;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Api.Client;

/// <summary>
/// Manager class for client addons.
/// </summary>
internal class ClientAddonManager {
    /// <summary>
    /// A list of addons that were registered by an assembly outside of HKMP. These addons still
    /// need to be initialized with the client API.
    /// </summary>
    private static readonly List<ClientAddon> RegisteredAddons;

    /// <summary>
    /// A boolean indicating whether addon loading has already occurred. If so, it is not possible to
    /// register new addons.
    /// </summary>
    private static bool _hasLoaded;

    /// <summary>
    /// The client API instance to pass to addons.
    /// </summary>
    private readonly ClientApi _clientApi;

    /// <summary>
    /// The mod settings instance for storing disabled addons.
    /// </summary>
    private readonly ModSettings _modSettings;

    /// <summary>
    /// A list of all loaded addons, the order is important as it is the exact order
    /// in which we sent it to the server and are expected to act on when receiving a response.
    /// </summary>
    private readonly List<ClientAddon> _addons;

    /// <summary>
    /// A dictionary of all networked addons indexed by name and version.
    /// </summary>
    private readonly Dictionary<(string, string), ClientAddon> _networkedAddons;

    /// <summary>
    /// Static constructor that initializes the list for addons registered outside of HKMP.
    /// </summary>
    static ClientAddonManager() {
        RegisteredAddons = new List<ClientAddon>();
    }

    /// <summary>
    /// Construct the addon manager with the client API.
    /// </summary>
    /// <param name="clientApi">The client API instance.</param>
    /// <param name="modSettings">The mod setting instance.</param>
    public ClientAddonManager(ClientApi clientApi, ModSettings modSettings) {
        _clientApi = clientApi;
        _modSettings = modSettings;

        _addons = new List<ClientAddon>();
        _networkedAddons = new Dictionary<(string, string), ClientAddon>();
    }

    /// <summary>
    /// Start loading addons from assemblies and initialize all known addons (both loaded and registered).
    /// </summary>
    public void LoadAddons() {
        // Since we are starting to load and initialize addons it is no longer possible for new addons to be
        // registered, so we denote that by setting the static boolean
        _hasLoaded = true;

        // Create an addon loader and load all addons in assemblies
        var addonLoader = new ClientAddonLoader();
        var addons = addonLoader.LoadAddons();

        // Now we add the addons that were registered by assemblies outside of HKMP
        addons.AddRange(RegisteredAddons);

        // Keep track of currently loaded addon names, so we can prevent duplicates
        var loadedAddons = new HashSet<string>();

        foreach (var addon in addons) {
            var addonName = addon.GetName();

            if (loadedAddons.Contains(addonName)) {
                Logger.Warn(
                    $"Could not initialize addon {addonName}, because an addon with the same name was already loaded");
                continue;
            }

            Logger.Info($"Initializing client addon: {addonName} {addon.GetVersion()}");

            try {
                addon.InternalInitialize(_clientApi);
            } catch (Exception e) {
                Logger.Warn(
                    $"Could not initialize addon {addon.GetName()}, exception:\n{e}");
                continue;
            }

            // Check if this addon was saved in the mod settings as disabled and then re-disable it
            if (
                addon is TogglableClientAddon togglableAddon &&
                _modSettings.DisabledAddons.Contains(addon.GetName())
            ) {
                togglableAddon.Disabled = true;
            }

            _addons.Add(addon);

            if (addon.NeedsNetwork) {
                _networkedAddons.Add((addonName, addon.GetVersion()), addon);
            }

            loadedAddons.Add(addonName);
        }
    }

    /// <summary>
    /// Try and get the networked client addon with the given name and version.
    /// </summary>
    /// <param name="name">The name of the addon.</param>
    /// <param name="version">The version of the addon.</param>
    /// <param name="addon">The client addon if it exists, null otherwise.</param>
    /// <returns>True if the client addon was found, false otherwise.</returns>
    public bool TryGetNetworkedAddon(string name, string version, out ClientAddon addon) {
        return _networkedAddons.TryGetValue((name, version), out addon);
    }

    /// <summary>
    /// Get a list of addon data for all networked addons.
    /// </summary>
    /// <returns>A list of AddonData instances.</returns>
    public List<AddonData> GetNetworkedAddonData() {
        var addonData = new List<AddonData>();

        foreach (var addon in _networkedAddons.Values) {
            if (addon is TogglableClientAddon { Disabled: true }) {
                continue;
            }

            addonData.Add(new AddonData(addon.GetName(), addon.GetVersion()));
        }

        return addonData;
    }

    /// <summary>
    /// Get a read-only list of all loaded addons.
    /// </summary>
    /// <returns>A read-only list of <see cref="ClientAddon"/> instances.</returns>
    public IReadOnlyList<ClientAddon> GetLoadedAddons() => _addons;

    /// <summary>
    /// Updates the order of all networked addons according to the given order.
    /// </summary>
    /// <param name="addonOrder">A byte array containing the IDs the addons should have.</param>
    public void UpdateNetworkedAddonOrder(byte[] addonOrder) {
        var index = 0;

        // The order of the addons in our local list should stay the same
        // between connection and obtaining the addon order from the server
        foreach (var addon in _networkedAddons.Values) {
            // Skip addons that are disabled
            if (addon is TogglableClientAddon { Disabled: true }) {
                continue;
            }

            // Retrieve the ID that this networked addon should have
            var id = addonOrder[index++];

            // Set the internal ID of the addon
            addon.Id = id;

            // If the addon has a network receiver registered, we will now commit the packet handlers, because
            // the addon has just received its ID
            if (addon.NetworkReceiver != null) {
                var networkReceiver = (ClientAddonNetworkReceiver) addon.NetworkReceiver;
                networkReceiver.CommitPacketHandlers();
            }

            Logger.Info($"Retrieved addon {addon.GetName()} v{addon.GetVersion()} ID: {id}");
        }
    }

    /// <summary>
    /// Clears the IDs of all networked addons.
    /// </summary>
    public void ClearNetworkedAddonIds() {
        foreach (var addon in _addons) {
            // We only check if the addon has an ID assigned, and remove it if so
            if (addon.Id.HasValue) {
                addon.Id = null;
            }
        }
    }

    /// <summary>
    /// Try to enable the addon with the given name.
    /// </summary>
    /// <param name="addonName">The name of the addon to enable.</param>
    /// <returns>True if the addon with the given name was enabled; otherwise false.</returns>
    public bool TryEnableAddon(string addonName) {
        foreach (var addon in _addons) {
            if (addon.GetName() == addonName) {
                if (addon is not TogglableClientAddon togglableAddon) {
                    return false;
                }

                togglableAddon.Disabled = false;

                _modSettings.DisabledAddons.Remove(addon.GetName());

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Try to disable the addon with the given name.
    /// </summary>
    /// <param name="addonName">The name of the addon to disable.</param>
    /// <returns>True if the addon with the given name was disable; otherwise false.</returns>
    public bool TryDisableAddon(string addonName) {
        foreach (var addon in _addons) {
            if (addon.GetName() == addonName) {
                if (addon is not TogglableClientAddon togglableAddon) {
                    return false;
                }

                togglableAddon.Disabled = true;

                _modSettings.DisabledAddons.Add(addon.GetName());

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Register an addon class from outside of HKMP.
    /// </summary>
    /// <param name="clientAddon">The client addon instance.</param>
    public static void RegisterAddon(ClientAddon clientAddon) {
        if (_hasLoaded) {
            throw new InvalidOperationException("Addon can not be registered at this moment");
        }

        RegisteredAddons.Add(clientAddon);
    }
}
