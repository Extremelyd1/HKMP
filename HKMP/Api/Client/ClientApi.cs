using Hkmp.Api.Client.Networking;
using Hkmp.Api.Command.Client;

namespace Hkmp.Api.Client {
    /// <summary>
    /// Client API interface implementation.
    /// </summary>
    internal class ClientApi : IClientApi {
        /// <inheritdoc/>
        public IClientManager ClientManager { get; }
        /// <inheritdoc/>
        public IClientCommandManager CommandManager { get; }
        /// <inheritdoc/>
        public IUiManager UiManager { get; }
        /// <inheritdoc/>
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