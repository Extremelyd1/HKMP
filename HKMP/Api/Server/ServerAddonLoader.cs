using System.Collections.Generic;
using Hkmp.Api.Addon;

namespace Hkmp.Api.Server {
    public class ServerAddonLoader : AddonLoader {
        private readonly IServerApi _serverApi;

        public ServerAddonLoader(IServerApi serverApi) {
            _serverApi = serverApi;
        }

        public List<ServerAddon> LoadAddons() {
            return LoadAddons<ServerAddon, IServerApi>(_serverApi);
        }
    }
}