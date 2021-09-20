using System.Collections.Generic;
using Hkmp.Api.Addon;

namespace Hkmp.Api.Client {
    public class ClientAddonLoader : AddonLoader {
        private readonly IClientApi _clientApi;

        public ClientAddonLoader(IClientApi clientApi) {
            _clientApi = clientApi;
        }

        public List<ClientAddon> LoadAddons() {
            return LoadAddons<ClientAddon, IClientApi>(_clientApi);
        }
    }
}