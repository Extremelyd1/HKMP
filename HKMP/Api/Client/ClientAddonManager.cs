namespace Hkmp.Api.Client {
    public class ClientAddonManager {
        public ClientAddonStorage AddonStorage { get; }

        public ClientAddonManager(ClientApi clientApi) {
            AddonStorage = new ClientAddonStorage();

            var addonLoader = new ClientAddonLoader(clientApi);

            var addons = addonLoader.LoadAddons();
            foreach (var addon in addons) {
                AddonStorage.AddAddon(addon);
                
                Logger.Get().Info(this, 
                    $"Initializing client addon: {addon.GetName()} {addon.GetVersion()}");
                
                addon.Initialize();
            }
        }
    }
}