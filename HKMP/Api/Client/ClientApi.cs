using Hkmp.Api.Client.Networking;
using Hkmp.Api.Command.Client;

namespace Hkmp.Api.Client {
    public class ClientApi : IClientApi {
        public IClientManager ClientManager { get; }
        public IClientCommandManager CommandManager { get; }
        public IUiManager UiManager { get; }
        public INetClient NetClient { get; }

        public ClientApi(
            IClientManager clientManager,
            IClientCommandManager commandManager,
            IUiManager uiManager,
            INetClient netClient
        ) {
            ClientManager = clientManager;
            CommandManager = commandManager;
            UiManager = uiManager;
            NetClient = netClient;
        }
    }
}