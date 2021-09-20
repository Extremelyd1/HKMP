using Hkmp.Api.Addon;

namespace Hkmp.Api.Client {
    public abstract class ClientAddon : IAddon {
        private readonly IClientApi _clientApi;
    
        public abstract string Identifier { get; }
        
        public abstract string Version { get; }
        
        public abstract bool NeedsNetwork { get; }
        
        public abstract void Initialize();

        public ClientAddon(IClientApi clientApi) {
            _clientApi = clientApi;
        }

        protected IClientApi GetClientApi() {
            return _clientApi;
        }

        protected ILogger GetLogger() {
            return Logger.Get();
        }
    }
}