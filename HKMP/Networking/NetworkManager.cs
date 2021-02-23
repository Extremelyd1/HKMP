using HKMP.Networking.Client;
using HKMP.Networking.Packet;
using HKMP.Networking.Server;

namespace HKMP.Networking {
    public class NetworkManager {
        public const int LocalUdpPort = 26951;

        private readonly NetClient _netClient;
        private readonly NetServer _netServer;

        public NetworkManager(PacketManager packetManager) {
            _netClient = new NetClient(packetManager);
            _netServer = new NetServer(packetManager);
        }

        public void StartServer(int port) {
            // Stop existing server
            if (_netServer.IsStarted) {
                _netServer.Stop();
            }

            // Start server again with given port
            _netServer.Start(port);
        }

        public void StopServer() {
            if (_netServer.IsStarted) {
                _netServer.Stop();
            } else {
                Logger.Warn(this, "Could not stop server, it was not started");
            }
        }

        public void ConnectClient(string ip, int port) {
            // Stop existing client
            if (_netClient.IsConnected) {
                _netClient.Disconnect();
            }
            
            // Connect client with given ip and port
            _netClient.Connect(ip, port);
        }

        public void DisconnectClient() {
            if (_netClient.IsConnected) {
                _netClient.Disconnect();
            } else {
                Logger.Warn(this, "Could not disconnect client, it was not connected");
            }
        }
    }
}