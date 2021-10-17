namespace Hkmp.Api.Server {
    public class ServerApi : IServerApi {

        private readonly IServerManager _serverManager;

        public ServerApi(IServerManager serverManager) {
            _serverManager = serverManager;
        }

        public IServerManager GetServerManager() {
            return _serverManager;
        }

        internal IServerApi GetCopy() {
            return new ServerApi(_serverManager);
        }
    }
}