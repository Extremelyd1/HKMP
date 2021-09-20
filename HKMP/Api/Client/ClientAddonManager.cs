using System.Collections.Generic;

namespace Hkmp.Api.Client {
    public class ClientAddonManager {
        private readonly List<ClientAddon> _addons;

        public ClientAddonManager(IClientApi clientApi) {
            var addonLoader = new ClientAddonLoader(clientApi);

            _addons = addonLoader.LoadAddons();
            
            InitializeAddons();
        }

        private void InitializeAddons() {
            foreach (var addon in _addons) {
                Logger.Get().Info(this, $"Initializing client addon: {addon.Identifier} {addon.Version}");
                
                addon.Initialize();
            }
        }
    }
}