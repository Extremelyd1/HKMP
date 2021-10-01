using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Api.Client {
    public class ClientAddonStorage {
        // A list of all loaded addons, the order is important as it is the exact order
        // in which we sent it to the server and are expected to act on when receiving a response
        private readonly List<ClientAddon> _addons;
        
        private readonly Dictionary<byte, ClientAddon> _networkedAddonsById;

        public ClientAddonStorage() {
            _addons = new List<ClientAddon>();
            _networkedAddonsById = new Dictionary<byte, ClientAddon>();
        }

        public void AddAddon(ClientAddon addon) {
            _addons.Add(addon);
        }
        
        public List<AddonData> GetNetworkedAddonData() {
            var addonData = new List<AddonData>();

            foreach (var addon in _networkedAddonsById.Values) {
                addonData.Add(new AddonData {
                    Identifier = addon.GetName(),
                    Version = addon.GetVersion()
                });
            }

            return addonData;
        }

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

                // Set the ID and add it to the dictionary
                addon.Id = id;
                _networkedAddonsById[id] = addon;
                
                Logger.Get().Info(this, $"Retrieved addon {addon.GetName()} v{addon.GetVersion()} ID: {id}");
            }
        }
    }
}