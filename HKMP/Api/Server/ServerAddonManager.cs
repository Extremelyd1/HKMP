using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Api.Server {
    /// <summary>
    /// Manager class for server addons.
    /// </summary>
    internal class ServerAddonManager {
        /// <summary>
        /// A list of addons that were registered by an assembly outside of HKMP. These addons still
        /// need to be initialized with the server API.
        /// </summary>
        private static readonly List<ServerAddon> RegisteredAddons;

        /// <summary>
        /// A boolean indicating whether addon loading already occurred. If so, it is not possible to
        /// register new addons.
        /// </summary>
        private static bool _hasLoaded;

        /// <summary>
        /// The server API instance to pass to addons.
        /// </summary>
        private readonly ServerApi _serverApi;

        /// <summary>
        /// A dictionary of all networked addons indexed by name and version.
        /// </summary>
        private readonly Dictionary<(string, string), ServerAddon> _networkedAddon;

        /// <summary>
        /// Static constructor that initializes the list of addons registered outside of HKMP.
        /// </summary>
        static ServerAddonManager() {
            RegisteredAddons = new List<ServerAddon>();
        }

        /// <summary>
        /// Construct the addon manager with the server API.
        /// </summary>
        /// <param name="serverApi">The server API instance.</param>
        public ServerAddonManager(ServerApi serverApi) {
            _serverApi = serverApi;

            _networkedAddon = new Dictionary<(string, string), ServerAddon>();
        }

        /// <summary>
        /// Start loading addons from assemblies and initialize all known addons (both loaded and registered).
        /// </summary>
        public void LoadAddons() {
            // Since we are starting to load and initialize addons it is no longer possible for new addons to be
            // registered, so we denote that by setting the static boolean
            _hasLoaded = true;

            // Create an addon loader and load all addons in assemblies
            var addonLoader = new ServerAddonLoader();
            var addons = addonLoader.LoadAddons();

            // Now we add the addons that were registered by assemblies outside of HKMP
            addons.AddRange(RegisteredAddons);

            // Keep track of currently loaded addon names, so we can prevent duplicates
            var loadedAddons = new HashSet<string>();

            byte lastId = 0;
            foreach (var addon in addons) {
                var addonName = addon.GetName();

                if (loadedAddons.Contains(addonName)) {
                    Logger.Get().Warn(this,
                        $"Could not initialize addon {addonName}, because an addon with the same name was already loaded");
                    continue;
                }

                if (addon.NeedsNetwork) {
                    addon.Id = lastId;

                    _networkedAddon[(addon.GetName(), addon.GetVersion())] = addon;

                    Logger.Get().Info(this, $"Assigned addon {addon.GetName()} v{addon.GetVersion()} ID: {lastId}");

                    lastId++;
                }

                Logger.Get().Info(this, $"Initializing server addon: {addon.GetName()} {addon.GetVersion()}");

                try {
                    addon.InternalInitialize(_serverApi);
                } catch (Exception e) {
                    Logger.Get().Warn(this,
                        $"Could not initialize addon {addon.GetName()}, exception: {e.GetType()}, {e.Message}, {e.StackTrace}");

                    // If the initialize failed, we remove it again from the networked addon dict
                    _networkedAddon.Remove((addon.GetName(), addon.GetVersion()));

                    continue;
                }

                loadedAddons.Add(addonName);
            }
        }

        /// <summary>
        /// Try and get the networked server addon with the given name and version.
        /// </summary>
        /// <param name="name">The name of the addon.</param>
        /// <param name="version">The version of the addon.</param>
        /// <param name="addon">The server addon if it exists, null otherwise.</param>
        /// <returns>True if the server addon was found, false otherwise.</returns>
        public bool TryGetNetworkedAddon(string name, string version, out ServerAddon addon) {
            return _networkedAddon.TryGetValue((name, version), out addon);
        }

        /// <summary>
        /// Get a list of addon data for all networked addons.
        /// </summary>
        /// <returns>A list of AddonData instances.</returns>
        public List<AddonData> GetNetworkedAddonData() {
            var addonData = new List<AddonData>();

            foreach (var addon in _networkedAddon.Values) {
                addonData.Add(new AddonData {
                    Identifier = addon.GetName(),
                    Version = addon.GetVersion()
                });
            }

            return addonData;
        }

        /// <summary>
        /// Register and addon class from outside of HKMP.
        /// </summary>
        /// <param name="serverAddon">The server addon instance.</param>
        public static void RegisterAddon(ServerAddon serverAddon) {
            if (_hasLoaded) {
                throw new InvalidOperationException("Addon can not be registered at this moment");
            }

            RegisteredAddons.Add(serverAddon);
        }
    }
}