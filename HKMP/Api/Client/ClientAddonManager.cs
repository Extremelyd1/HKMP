using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Api.Client {
    /// <summary>
    /// Manager class for client addons.
    /// </summary>
    public class ClientAddonManager {
        /// <summary>
        /// A list of all loaded addons, the order is important as it is the exact order
        /// in which we sent it to the server and are expected to act on when receiving a response.
        /// </summary>
        private readonly List<ClientAddon> _addons;
        
        public ClientAddonManager(ClientApi clientApi) {
            _addons = new List<ClientAddon>();

            var addonLoader = new ClientAddonLoader(clientApi);

            var addons = addonLoader.LoadAddons();
            foreach (var addon in addons) {
                _addons.Add(addon);
                
                Logger.Get().Info(this, 
                    $"Initializing client addon: {addon.GetName()} {addon.GetVersion()}");
                
                addon.Initialize();
            }
        }
        
        /// <summary>
        /// Get a list of addon data for all networked addons.
        /// </summary>
        /// <returns>A list of AddonData instances.</returns>
        public List<AddonData> GetNetworkedAddonData() {
            var addonData = new List<AddonData>();

            foreach (var addon in _addons) {
                if (!addon.NeedsNetwork) {
                    continue;
                }
                
                addonData.Add(new AddonData {
                    Identifier = addon.GetName(),
                    Version = addon.GetVersion()
                });
            }

            return addonData;
        }
        
        /// <summary>
        /// Updates the order of all networked addons according to the given order.
        /// </summary>
        /// <param name="addonOrder">A byte array containing the IDs the addons should have.</param>
        public void UpdateNetworkedAddonOrder(byte[] addonOrder) {
            var index = 0;

            // The order of the addons in our local list should stay the same
            // between connection and obtaining the addon order from the server
            foreach (var addon in _addons) {
                // Skip all non-networked addons
                if (!addon.NeedsNetwork) {
                    continue;
                }

                // Retrieve the ID that this networked addon should have
                var id = addonOrder[index++];

                // Set the internal ID of the addon
                addon.Id = id;
                
                Logger.Get().Info(this, $"Retrieved addon {addon.GetName()} v{addon.GetVersion()} ID: {id}");
            }
        }
    }
}