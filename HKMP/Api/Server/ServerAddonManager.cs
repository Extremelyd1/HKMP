using System.Collections.Generic;

namespace Hkmp.Api.Server {
    public class ServerAddonManager {
        private readonly List<ServerAddon> _addons;

        public ServerAddonManager(IServerApi serverApi) {
            var addonLoader = new ServerAddonLoader(serverApi);

            _addons = addonLoader.LoadAddons();

            InitializeAddons();
        }
        
        private void InitializeAddons() {
            foreach (var addon in _addons) {
                Logger.Get().Info(this, $"Initializing server addon: {addon.Identifier} {addon.Version}");
                
                addon.Initialize();
            }
        }
    }
}