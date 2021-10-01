namespace Hkmp.Api.Server {
    public class ServerAddonManager {
        public ServerAddonStorage AddonStorage { get; }
        
        public ServerAddonManager(IServerApi serverApi) {
            AddonStorage = new ServerAddonStorage();
            
            var addonLoader = new ServerAddonLoader(serverApi);

            foreach (var addon in addonLoader.LoadAddons()) {
                AddonStorage.AddAddon(addon);
                
                Logger.Get().Info(this, 
                    $"Initializing server addon: {addon.GetName()} {addon.GetVersion()}");
                
                addon.Initialize();
            }
        }
    }
}