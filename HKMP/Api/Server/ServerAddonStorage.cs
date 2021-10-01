using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;

namespace Hkmp.Api.Server {
    public class ServerAddonStorage {
        private byte _lastId;

        private readonly Dictionary<byte, ServerAddon> _networkedAddonsById;

        private readonly Dictionary<(string, string), ServerAddon> _networkedAddonsByNameVersion;

        public ServerAddonStorage() {
            _lastId = 0;

            _networkedAddonsById = new Dictionary<byte, ServerAddon>();
            _networkedAddonsByNameVersion = new Dictionary<(string, string), ServerAddon>();
        }

        public void AddAddon(ServerAddon addon) {
            if (addon.NeedsNetwork) {
                addon.Id = _lastId;
                
                _networkedAddonsById[_lastId] = addon;

                _networkedAddonsByNameVersion[(addon.GetName(), addon.GetVersion())] = addon;
                
                Logger.Get().Info(this, $"Assigned addon {addon.GetName()} v{addon.GetVersion()} ID: {_lastId}");

                _lastId++;
            }
        }

        public bool TryGetNetworkedAddon(byte id, out ServerAddon addon) {
            return _networkedAddonsById.TryGetValue(id, out addon);
        }

        public bool TryGetNetworkedAddon(string name, string version, out ServerAddon addon) {
            return _networkedAddonsByNameVersion.TryGetValue((name, version), out addon);
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

    }
}