namespace Hkmp.Api.Client {
    public class ClientApi : IClientApi {

        public IClientManager ClientManager { get; }
        public INetClient NetClient { get; }

        public ClientApi(IClientManager clientManager, INetClient netClient) {
            ClientManager = clientManager;
            NetClient = netClient;
        }
    }
}