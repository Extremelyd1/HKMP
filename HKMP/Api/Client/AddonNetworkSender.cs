using System;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client {
    public class AddonNetworkSender<TPacketId> : 
        AddonNetworkTransmitter<TPacketId>, 
        IAddonNetworkSender<TPacketId> 
        where TPacketId : Enum {
        
        private readonly NetClient _netClient;
        private readonly ClientAddon _clientAddon;

        private readonly byte _packetIdSize;

        public AddonNetworkSender(
            NetClient netClient, 
            ClientAddon clientAddon
        ) {
            _netClient = netClient;
            _clientAddon = clientAddon;
            
            _packetIdSize = (byte) PacketIdDict.Count;
        }
    
        public void SendSingleData(TPacketId packetId, IPacketData packetData) {
            if (!_netClient.IsConnected) {
                throw new InvalidOperationException("NetClient is not connected, cannot send data");
            }

            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    "Given packet ID was not part of enum when creating this network sender");
            }

            _netClient.UpdateManager.SetAddonData(
                _clientAddon.Id, 
                idValue,
                _packetIdSize,
                packetData
            );
        }

        public void SendCollectionData<TPacketData>(
            TPacketId packetId, 
            IPacketData packetData
        ) where TPacketData : IPacketData, new() {
            if (!_netClient.IsConnected) {
                throw new InvalidOperationException("NetClient is not connected, cannot send data");
            }
            
            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    "Given packet ID was not part of enum when creating this network sender");
            }

            _netClient.UpdateManager.SetAddonDataAsCollection<TPacketData>(
                _clientAddon.Id, 
                idValue,
                _packetIdSize,
                packetData
            );
        }
    }
}