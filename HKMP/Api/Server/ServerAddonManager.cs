using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Api.Server {
    /// <summary>
    /// Manager class for server addons.
    /// </summary>
    public class ServerAddonManager {
        /// <summary>
        /// A dictionary of all networked addons indexed by name and version.
        /// </summary>
        private readonly Dictionary<(string, string), ServerAddon> _networkedAddon;
        
        public ServerAddonManager(ServerApi serverApi) {
            _networkedAddon = new Dictionary<(string, string), ServerAddon>();
            
            var addonLoader = new ServerAddonLoader(serverApi);

            byte lastId = 0;
            foreach (var addon in addonLoader.LoadAddons()) {
                if (addon.NeedsNetwork) {
                    addon.Id = lastId;
                
                    _networkedAddon[(addon.GetName(), addon.GetVersion())] = addon;
                
                    Logger.Get().Info(this, $"Assigned addon {addon.GetName()} v{addon.GetVersion()} ID: {lastId}");

                    lastId++;
                }
                
                Logger.Get().Info(this, $"Initializing server addon: {addon.GetName()} {addon.GetVersion()}");

                try {
                    addon.Initialize();
                } catch (Exception e) {
                    Logger.Get().Warn(this, $"Could not initialize addon {addon.GetName()}, exception: {e.GetType()}, {e.Message}, {e.StackTrace}");
                    
                    // If the initialize failed, we remove it again from the networked addon dict
                    _networkedAddon.Remove((addon.GetName(), addon.GetVersion()));
                }
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
    }
}