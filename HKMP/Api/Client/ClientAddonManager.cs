using System;
using System.Collections.Generic;
using Hkmp.Api.Client.Networking;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Api.Client {
    /// <summary>
    /// Manager class for client addons.
    /// </summary>
    internal class ClientAddonManager {
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
                Logger.Get().Info(this, 
                    $"Initializing client addon: {addon.GetName()} {addon.GetVersion()}");

                try {
                    addon.Initialize();
                } catch (Exception e) {
                    Logger.Get().Warn(this, $"Could not initialize addon {addon.GetName()}, exception: {e.GetType()}, {e.Message}, {e.StackTrace}");
                    continue;
                }
                
                _addons.Add(addon);
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

                // If the addon has a network receiver registered, we will now commit the packet handlers, because
                // the addon has just received its ID
                if (addon.NetworkReceiver != null) {
                    var networkReceiver = (ClientAddonNetworkReceiver)addon.NetworkReceiver;
                    networkReceiver.CommitPacketHandlers();
                }
                
                Logger.Get().Info(this, $"Retrieved addon {addon.GetName()} v{addon.GetVersion()} ID: {id}");
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
    }
}