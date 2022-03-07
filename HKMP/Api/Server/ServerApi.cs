using Hkmp.Api.Command.Server;
using Hkmp.Api.Server.Networking;

namespace Hkmp.Api.Server {
    public class ServerApi : IServerApi {

        public IServerManager ServerManager { get; }
        public IServerCommandManager CommandManager { get; }
        public INetServer NetServer { get; }

        public ServerApi(
            IServerManager serverManager,
            IServerCommandManager commandManager,
            INetServer netServer
        ) {
            ServerManager = serverManager;
            CommandManager = commandManager;
            NetServer = netServer;
        }
    }
}