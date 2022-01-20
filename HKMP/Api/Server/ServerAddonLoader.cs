using System.Collections.Generic;
using Hkmp.Api.Addon;

namespace Hkmp.Api.Server {
    /// <summary>
    /// Addon loader for the server-side.
    /// </summary>
    public class ServerAddonLoader : AddonLoader {
        /// <summary>
        /// The server API instance to pass onto newly loaded addons.
        /// </summary>
        private readonly ServerApi _serverApi;

        public ServerAddonLoader(ServerApi serverApi) {
            _serverApi = serverApi;
        }

        /// <summary>
        /// Loads all server addons.
        /// </summary>
        /// <returns>A list of ServerAddon instances.</returns>
        public List<ServerAddon> LoadAddons() {
            return LoadAddons<ServerAddon, IServerApi>(_serverApi);
        }
    }
}