using System;
using Hkmp.Api.Client;
using Hkmp.Api.Client.Networking;
using Hkmp.Networking.Packet;

namespace Hkmp.Api.Server.Networking {
    /// <summary>
    /// Implementation of the server-side network receiver for addons.
    /// </summary>
    public class ServerAddonNetworkReceiver<TPacketId> :
        AddonNetworkTransmitter<TPacketId>,
        IServerAddonNetworkReceiver<TPacketId>
    where TPacketId : Enum {
        /// <summary>
        /// The instance of the server addon that this network receiver belongs to.
        /// </summary>
        private readonly ServerAddon _serverAddon;
        /// <summary>
        /// The packet manager used to register packet handlers for the addon.
        /// </summary>
        private readonly PacketManager _packetManager;

        public ServerAddonNetworkReceiver(
            ServerAddon serverAddon,
            PacketManager packetManager
        ) {
            _serverAddon = serverAddon;
            _packetManager = packetManager;
        }


        public void RegisterPacketHandler(TPacketId packetId, Action<ushort> handler) {
            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    "Given packet ID was not part of enum when creating this network receiver");
            }
            
            _packetManager.RegisterServerAddonPacketHandler(
                _serverAddon.Id,
                idValue,
                (id, _) => handler(id)
            );
        }

        public void RegisterPacketHandler<TPacketData>(TPacketId packetId, GenericServerPacketHandler<TPacketData> handler) where TPacketData : IPacketData {
            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    "Given packet ID was not part of enum when creating this network receiver");
            }
            
            _packetManager.RegisterServerAddonPacketHandler(
                _serverAddon.Id,
                idValue,
                (id, iPacketData) => handler(id, (TPacketData) iPacketData)
            );
        }

        public void DeregisterPacketHandler(TPacketId packetId) {
            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    "Given packet ID was not part of enum when creating this network receiver");
            }
            
            _packetManager.DeregisterServerAddonPacketHandler(_serverAddon.Id, idValue);
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