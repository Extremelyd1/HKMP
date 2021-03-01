using System;
using HKMP.Networking.Client;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
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

        /**
         * Starts the server on the given port
         */
        public void StartServer(int port) {
            // Stop existing server
            if (_netServer.IsStarted) {
                _netServer.Stop();
            }

            // Start server again with given port
            _netServer.Start(port);
        }

        /**
         * Stops the server and notifies all clients of shutdown
         */
        public void StopServer() {
            if (_netServer.IsStarted) {
                // Before shutting down, send TCP packets to all clients indicating
                // that the server is shutting down
                _netServer.BroadcastTcp(new ServerShutdownPacket());
                
                _netServer.Stop();
            } else {
                Logger.Warn(this, "Could not stop server, it was not started");
            }
        }

        /**
         * Tries to establish a connection with the server at the given IP and port
         */
        public void ConnectClient(string ip, int port) {
            // Stop existing client
            if (_netClient.IsConnected) {
                _netClient.Disconnect();
            }
            
            // Connect client with given ip and port
            _netClient.Connect(ip, port);
        }

        public void RegisterOnConnect(Action onConnect) {
            _netClient.RegisterOnConnect(onConnect);
        }

        public void RegisterOnConnectFailed(Action onConnectFailed) {
            _netClient.RegisterOnConnectFailed(onConnectFailed);
        }

        /**
         * Disconnect the local client from the server
         */
        public void DisconnectClient() {
            if (_netClient.IsConnected) {
                // First send the server that we are disconnecting
                _netClient.SendTcp(new PlayerDisconnectPacket());
                
                // Then actually disconnect
                _netClient.Disconnect();
            } else {
                Logger.Warn(this, "Could not disconnect client, it was not connected");
            }
        }

        public NetClient GetNetClient() {
            return _netClient;
        }

        public NetServer GetNetServer() {
            return _netServer;
        }
    }
}