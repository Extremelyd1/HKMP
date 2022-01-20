using System;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Client {
    /// <summary>
    /// Implementation of client-side network receiver for addons.
    /// </summary>
    /// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
    public class ClientAddonNetworkReceiver<TPacketId> : 
        AddonNetworkTransmitter<TPacketId>,
        IClientAddonNetworkReceiver<TPacketId> 
        where TPacketId : Enum {

        /// <summary>
        /// The instance of the client addon that this network sender belongs to.
        /// </summary>
        private readonly ClientAddon _clientAddon;
        /// <summary>
        /// The packet manager used to register packet handlers for the addon.
        /// </summary>
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

        /// <summary>
        /// Transform a given function that instantiates a IPacketData from a given enum value into a function
        /// that instead requires a byte as parameter.
        /// </summary>
        /// <param name="packetInstantiator">The existing instantiator function that takes an enum value.</param>
        /// <returns>New instantiator function that takes a byte as parameter.</returns>
        internal Func<byte, IPacketData> TransformPacketInstantiator(
            Func<TPacketId, IPacketData> packetInstantiator
        ) {
            return byteId => packetInstantiator(ReversePacketIdDict[byteId]);
        }
    }
}