using Hkmp.Api.Command;

namespace Hkmp.Api.Client {
    public class ClientApi : IClientApi {
        public IClientManager ClientManager { get; }
        public ICommandManager CommandManager { get; }
        public IUiManager UiManager { get; }
        public INetClient NetClient { get; }

        public ClientApi(
            IClientManager clientManager,
            ICommandManager commandManager,
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