using System;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client {
    /// <summary>
    /// Implementation of client-side network sender for addons.
    /// </summary>
    /// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
    public class AddonNetworkSender<TPacketId> : 
        AddonNetworkTransmitter<TPacketId>, 
        IAddonNetworkSender<TPacketId> 
        where TPacketId : Enum {
        
        /// <summary>
        /// The net client used to send data.
        /// </summary>
        private readonly NetClient _netClient;
        /// <summary>
        /// The instance of the client addon that this network sender belongs to.
        /// </summary>
        private readonly ClientAddon _clientAddon;

        /// <summary>
        /// The size of the packet ID space.
        /// </summary>
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
            TPacketData packetData
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