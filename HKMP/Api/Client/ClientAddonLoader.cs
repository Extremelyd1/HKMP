using System.Collections.Generic;
using Hkmp.Api.Addon;

namespace Hkmp.Api.Client {
    /// <summary>
    /// Addon loader for the client-side.
    /// </summary>
    public class ClientAddonLoader : AddonLoader {
        /// <summary>
        /// The client API instance to pass onto newly loaded addons.
        /// </summary>
        private readonly ClientApi _clientApi;

        public ClientAddonLoader(ClientApi clientApi) {
            _clientApi = clientApi;
        }

        /// <summary>
        /// Loads all client addons.
        /// </summary>
        /// <returns>A list of ClientAddon instances.</returns>
        public List<ClientAddon> LoadAddons() {
            return LoadAddons<ClientAddon, IClientApi>(_clientApi);
        }
    }
}