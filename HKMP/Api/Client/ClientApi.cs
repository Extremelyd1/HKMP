namespace Hkmp.Api.Client {
    public class ClientApi : IClientApi {

        public IClientManager ClientManager { get; }
        public IUiManager UiManager { get; }
        public INetClient NetClient { get; }

        public ClientApi(IClientManager clientManager, IUiManager uiManager, INetClient netClient) {
            ClientManager = clientManager;
            UiManager = uiManager;
            NetClient = netClient;
        }
    }
}