namespace Hkmp.Api.Client {
    public class ClientApi : IClientApi {

        private readonly IClientManager _clientManager;

        public ClientApi(IClientManager clientManager) {
            _clientManager = clientManager;
        }
        
        public IClientManager GetClientManager() {
            return _clientManager;
        }
    }
}