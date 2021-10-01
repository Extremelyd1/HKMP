namespace Hkmp.Api.Server {
    public abstract class ServerAddon : Addon.Addon {
        private readonly IServerApi _serverApi;
    
        protected abstract string Name { get; }
        
        protected abstract string Version { get; }
        
        public abstract bool NeedsNetwork { get; }
        
        public abstract void Initialize();

        public ServerAddon(IServerApi serverApi) {
            _serverApi = serverApi;
        }

        public string GetName() {
            if (Name.Length > MaxNameLength) {
                return Name.Substring(0, 20);
            }

            return Name;
        }
        
        public string GetVersion() {
            if (Version.Length > MaxVersionLength) {
                return Version.Substring(0, 20);
            }

            return Version;
        }

        protected IServerApi GetServerApi() {
            return _serverApi;
        }
        
        protected ILogger GetLogger() {
            return Logger.Get();
        }
    }
}