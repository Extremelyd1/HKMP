using System;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client {
    public class ClientAddonNetworkReceiver<TPacketId> : 
        AddonNetworkTransmitter<TPacketId>,
        IClientAddonNetworkReceiver<TPacketId> 
        where TPacketId : Enum {

        private readonly ClientAddon _clientAddon;
        private readonly PacketManager _packetManager;
        
        public ClientAddonNetworkReceiver(
            ClientAddon clientAddon, 
            PacketManager packetManager
        ) {
            _clientAddon = clientAddon;
            _packetManager = packetManager;
        }
        
        public void RegisterPacketHandler(TPacketId packetId, Action handler) {
            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    "Given packet ID was not part of enum when creating this network receiver");
            }
            
            _packetManager.RegisterClientAddonPacketHandler(
                _clientAddon.Id, 
                idValue, 
                _ => handler()
            );
        }
        
        public void RegisterPacketHandler<TPacketData>(
            TPacketId packetId,
            GenericClientPacketHandler<TPacketData> handler
        ) where TPacketData : IPacketData {
            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    "Given packet ID was not part of enum when creating this network receiver");
            }
            
            _packetManager.RegisterClientAddonPacketHandler(
                _clientAddon.Id, 
                idValue, 
                iPacketData => handler((TPacketData) iPacketData)
            );
        }
    }
}