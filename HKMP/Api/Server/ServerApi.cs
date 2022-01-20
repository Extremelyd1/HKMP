namespace Hkmp.Api.Server {
    public class ServerApi : IServerApi {

        public IServerManager ServerManager { get; }
        public INetServer NetServer { get; }

        public ServerApi(IServerManager serverManager, INetServer netServer) {
            ServerManager = serverManager;
            NetServer = netServer;
        }
    }
}