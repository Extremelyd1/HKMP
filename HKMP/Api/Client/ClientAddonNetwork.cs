using System;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client {
    public class ClientAddonNetwork : IClientAddonNetwork {
        private readonly NetClient _netClient;
        private readonly ClientAddon _clientAddon;

        public ClientAddonNetwork(NetClient netClient, ClientAddon clientAddon) {
            _netClient = netClient;
            _clientAddon = clientAddon;
        }
    
        public void SendData(IPacketData packetData) {
            if (_netClient.IsConnected) {
                throw new InvalidOperationException("NetClient is not connected, cannot send data");
            }

            var updateManager = _netClient.UpdateManager;
        }
    }
}