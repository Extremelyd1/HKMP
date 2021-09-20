using Hkmp.Api.Addon;

namespace Hkmp.Api.Server {
    public abstract class ServerAddon : IAddon {
        private readonly IServerApi _serverApi;
    
        public abstract string Identifier { get; }
        
        public abstract string Version { get; }
        
        public abstract bool NeedsNetwork { get; }
        
        public abstract void Initialize();

        public ServerAddon(IServerApi serverApi) {
            _serverApi = serverApi;
        }

        protected IServerApi GetServerApi() {
            return _serverApi;
        }
        
        protected ILogger GetLogger() {
            return Logger.Get();
        }
    }
}