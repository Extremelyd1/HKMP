namespace Hkmp.Api.Client {
    public class ClientApi : IClientApi {

        private readonly IClientManager _clientManager;
        private readonly INetClient _netClient;

        public ClientApi(IClientManager clientManager, INetClient netClient) {
            _clientManager = clientManager;
            _netClient = netClient;
        }
        
        public IClientManager GetClientManager() {
            return _clientManager;
        }
    }
}