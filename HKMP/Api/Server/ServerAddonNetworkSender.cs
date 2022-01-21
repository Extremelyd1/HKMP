using System;
using Hkmp.Api.Client;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Server;

namespace Hkmp.Api.Server {
    /// <summary>
    /// Implementation of server-side network sender for addons.
    /// </summary>
    /// <typeparam name="TPacketId">The type of the packet ID enum.</typeparam>
    public class ServerAddonNetworkSender<TPacketId> :
        AddonNetworkTransmitter<TPacketId>,
        IServerAddonNetworkSender<TPacketId>
        where TPacketId : Enum {
        /// <summary>
        /// The exception message for when data cannot be send because the server is not started.
        /// </summary>
        private const string ServerNotStartedExceptionMsg = "NetServer is not started, cannot send data";
        /// <summary>
        /// The exception message for when data cannot be send because the given packet ID is invalid.
        /// </summary>
        private const string PacketIdInvalidExceptionMsg =
            "Given packet ID was not part of enum when creating this network sender";
        
        /// <summary>
        /// The net server used to send data.
        /// </summary>
        private readonly NetServer _netServer;

        /// <summary>
        /// The instance of the server addon that this network sender belongs to.
        /// </summary>
        private readonly ServerAddon _serverAddon;

        /// <summary>
        /// The size of the packet ID space.
        /// </summary>
        private readonly byte _packetIdSize;

        public ServerAddonNetworkSender(
            NetServer netServer,
            ServerAddon serverAddon
        ) {
            _netServer = netServer;
            _serverAddon = serverAddon;

            _packetIdSize = (byte)PacketIdDict.Count;
        }

        public void SendSingleData(TPacketId packetId, IPacketData packetData, ushort playerId) {
            if (!_netServer.IsStarted) {
                throw new InvalidOperationException(ServerNotStartedExceptionMsg);
            }

            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    PacketIdInvalidExceptionMsg);
            }

            var updateManager = _netServer.GetUpdateManagerForClient(playerId);
            if (updateManager == null) {
                throw new InvalidOperationException($"Player with ID '{playerId}' is not connected");
            }

            updateManager.SetAddonData(
                _serverAddon.Id,
                idValue,
                _packetIdSize,
                packetData
            );
        }

        public void SendSingleData(TPacketId packetId, IPacketData packetData, params ushort[] playerIds) {
            foreach (var playerId in playerIds) {
                SendSingleData(packetId, packetData, playerId);
            }
        }

        public void BroadcastSingleData(TPacketId packetId, IPacketData packetData) {
            if (!_netServer.IsStarted) {
                throw new InvalidOperationException(ServerNotStartedExceptionMsg);
            }
            
            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    PacketIdInvalidExceptionMsg);
            }

            _netServer.SetDataForAllClients(updateManager => {
                updateManager?.SetAddonData(
                    _serverAddon.Id,
                    idValue,
                    _packetIdSize,
                    packetData
                );
            });
        }

        public void SendCollectionData<TPacketData>(
            TPacketId packetId, 
            TPacketData packetData, 
            ushort playerId
        ) where TPacketData : IPacketData, new() {
            if (!_netServer.IsStarted) {
                throw new InvalidOperationException(ServerNotStartedExceptionMsg);
            }
            
            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    PacketIdInvalidExceptionMsg);
            }
            
            var updateManager = _netServer.GetUpdateManagerForClient(playerId);
            if (updateManager == null) {
                throw new InvalidOperationException($"Player with ID '{playerId}' is not connected");
            }
            
            updateManager.SetAddonDataAsCollection(
                _serverAddon.Id,
                idValue,
                _packetIdSize,
                packetData
            );
        }

        public void SendCollectionData<TPacketData>(
            TPacketId packetId, 
            TPacketData packetData,
            params ushort[] playerIds
        ) where TPacketData : IPacketData, new() {
            foreach (var playerId in playerIds) {
                SendCollectionData(packetId, packetData, playerId);
            }
        }

        public void BroadcastCollectionData<TPacketData>(
            TPacketId packetId, 
            TPacketData packetData
        ) where TPacketData : IPacketData, new() {
            if (!_netServer.IsStarted) {
                throw new InvalidOperationException(ServerNotStartedExceptionMsg);
            }
            
            if (!PacketIdDict.TryGetValue(packetId, out var idValue)) {
                throw new InvalidOperationException(
                    PacketIdInvalidExceptionMsg);
            }

            _netServer.SetDataForAllClients(updateManager => {
                updateManager?.SetAddonDataAsCollection(
                    _serverAddon.Id,
                    idValue,
                    _packetIdSize,
                    packetData
                );
            });
        }
    }
}