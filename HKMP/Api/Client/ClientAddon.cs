namespace Hkmp.Api.Client {
    public abstract class ClientAddon : Addon.Addon {
        private readonly IClientApi _clientApi;
        
        internal object NetworkSender;
        internal object NetworkReceiver;

        protected abstract string Name { get; }
        
        protected abstract string Version { get; }
        
        public abstract bool NeedsNetwork { get; }
        
        public abstract void Initialize();

        public ClientAddon(IClientApi clientApi) {
            _clientApi = clientApi;
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

        protected IClientApi GetClientApi() {
            return _clientApi;
        }

        protected ILogger GetLogger() {
            return Logger.Get();
        }
    }
}