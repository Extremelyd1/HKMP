using System.Collections.Generic;
using Hkmp.Api.Addon;

namespace Hkmp.Api.Client {
    public class ClientAddonLoader : AddonLoader {
        private readonly ClientApi _clientApi;

        public ClientAddonLoader(ClientApi clientApi) {
            _clientApi = clientApi;
        }

        public List<ClientAddon> LoadAddons() {
            return LoadAddons<ClientAddon, IClientApi>(_clientApi);
        }
    }
}