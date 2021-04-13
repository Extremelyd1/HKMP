using System;
using System.Collections.Generic;
using HKMP.Util;
using UnityEngine;

namespace HKMP.Networking.Packet {
    public delegate void ClientPacketHandler(IPacketData packet);
    public delegate void GenericClientPacketHandler<in T>(T packet) where T : IPacketData;

    public delegate void EmptyServerPacketHandler(ushort id);
    public delegate void ServerPacketHandler(ushort id, IPacketData packet);
    public delegate void GenericServerPacketHandler<in T>(ushort id, T packet) where T : IPacketData;
    
    /**
     * Manages incoming packets by executing a corresponding registered handler
     */
    public class PacketManager {

        // Handlers that deal with data from the server intended for the client
        private readonly Dictionary<ClientPacketId, ClientPacketHandler> _clientPacketHandlers;
        // Handlers that deal with data from the client intended for the server
        private readonly Dictionary<ServerPacketId, ServerPacketHandler> _serverPacketHandlers;

        /**
         * Manages packets that are received by the given NetClient
         */
        public PacketManager() {
            _clientPacketHandlers = new Dictionary<ClientPacketId, ClientPacketHandler>();
            _serverPacketHandlers = new Dictionary<ServerPacketId, ServerPacketHandler>();
        }

        /**
         * Handle data received by a client
         */
        public void HandleClientPacket(ClientUpdatePacket packet) {

            /*foreach (var item in packet.DataPacketIds)
            {
                Logger.Info(this,$"client to handle {Enum.GetName(typeof(ClientPacketId), item)}");
            }*/
            // Execute corresponding packet handlers
            if (packet.DataPacketIds.Contains(ClientPacketId.PlayerConnect)) {
                foreach (var playerConnect in packet.PlayerConnect.DataInstances) {
                    ExecuteClientPacketHandler(ClientPacketId.PlayerConnect, playerConnect);
                }
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.PlayerDisconnect)) {
                foreach (var playerDisconnect in packet.PlayerDisconnect.DataInstances) {
                    ExecuteClientPacketHandler(ClientPacketId.PlayerDisconnect, playerDisconnect);
                }
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.ServerShutdown)) {
                ExecuteClientPacketHandler(ClientPacketId.ServerShutdown, null);
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.PlayerEnterScene)) {
                foreach (var playerEnterScene in packet.PlayerEnterScene.DataInstances) {
                    ExecuteClientPacketHandler(ClientPacketId.PlayerEnterScene, playerEnterScene);
                }
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.PlayerAlreadyInScene)) {
                ExecuteClientPacketHandler(ClientPacketId.PlayerAlreadyInScene, packet.PlayerAlreadyInScene);
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.PlayerLeaveScene)) {
                foreach (var playerLeaveScene in packet.PlayerLeaveScene.DataInstances) {
                    ExecuteClientPacketHandler(ClientPacketId.PlayerLeaveScene, playerLeaveScene);
                }
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.PlayerUpdate)) {
                foreach (var playerUpdate in packet.PlayerUpdates.DataInstances) {
                    ExecuteClientPacketHandler(ClientPacketId.PlayerUpdate, playerUpdate);
                }
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.EntityUpdate)) {
                foreach (var entityUpdate in packet.EntityUpdates.DataInstances) {
                    ExecuteClientPacketHandler(ClientPacketId.EntityUpdate, entityUpdate);
                }
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.PlayerDeath)) {
                foreach (var playerDeath in packet.PlayerDeath.DataInstances) {
                    ExecuteClientPacketHandler(ClientPacketId.PlayerDeath, playerDeath);
                }
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.PlayerTeamUpdate)) {
                foreach (var playerTeamUpdate in packet.PlayerTeamUpdate.DataInstances) {
                    ExecuteClientPacketHandler(ClientPacketId.PlayerTeamUpdate, playerTeamUpdate);
                }
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.PlayerSkinUpdate)) {
                foreach (var playerSkinUpdate in packet.PlayerSkinUpdate.DataInstances) {
                    ExecuteClientPacketHandler(ClientPacketId.PlayerSkinUpdate, playerSkinUpdate);
                }
            }
            
            if (packet.DataPacketIds.Contains(ClientPacketId.PlayerEmoteUpdate)) {
                foreach (var playerEmoteUpdate in packet.PlayerEmoteUpdate.DataInstances) {
                    ExecuteClientPacketHandler(ClientPacketId.PlayerEmoteUpdate, playerEmoteUpdate);
                }
            }

            if (packet.DataPacketIds.Contains(ClientPacketId.GameSettingsUpdated)) {
                ExecuteClientPacketHandler(ClientPacketId.GameSettingsUpdated, packet.GameSettingsUpdate);
            }
        }

        /**
         * Handle data received by the server
         */
        public void HandleServerPacket(ushort id, ServerUpdatePacket packet) {

            /*foreach (var item in packet.DataPacketIds)
            {
                Logger.Info(this,$"server to handle {Enum.GetName(typeof(ServerPacketId), item)}");
            }
            */
            // Execute corresponding packet handlers
            if (packet.DataPacketIds.Contains(ServerPacketId.HelloServer)) {
                ExecuteServerPacketHandler(id, ServerPacketId.HelloServer, packet.HelloServer);
            }

            if (packet.DataPacketIds.Contains(ServerPacketId.PlayerDisconnect)) {
                ExecuteServerPacketHandler(id, ServerPacketId.PlayerDisconnect, null);
            }

            if (packet.DataPacketIds.Contains(ServerPacketId.PlayerUpdate)) {
                ExecuteServerPacketHandler(id, ServerPacketId.PlayerUpdate, packet.PlayerUpdate);
            }

            if (packet.DataPacketIds.Contains(ServerPacketId.EntityUpdate)) {
                foreach (var entityUpdate in packet.EntityUpdates.DataInstances) {
                    ExecuteServerPacketHandler(id, ServerPacketId.EntityUpdate, entityUpdate);
                }
            }

            if (packet.DataPacketIds.Contains(ServerPacketId.PlayerEnterScene)) {
                ExecuteServerPacketHandler(id, ServerPacketId.PlayerEnterScene, packet.PlayerEnterScene);
            }

            if (packet.DataPacketIds.Contains(ServerPacketId.PlayerLeaveScene)) {
                ExecuteServerPacketHandler(id, ServerPacketId.PlayerLeaveScene, null);
            }

            if (packet.DataPacketIds.Contains(ServerPacketId.PlayerDeath)) {
                ExecuteServerPacketHandler(id, ServerPacketId.PlayerDeath, null);
            }

            if (packet.DataPacketIds.Contains(ServerPacketId.PlayerTeamUpdate)) {
                ExecuteServerPacketHandler(id, ServerPacketId.PlayerTeamUpdate, packet.PlayerTeamUpdate);
            }

            if (packet.DataPacketIds.Contains(ServerPacketId.PlayerSkinUpdate)) {
                ExecuteServerPacketHandler(id, ServerPacketId.PlayerSkinUpdate, packet.PlayerSkinUpdate);
            }
            
            if (packet.DataPacketIds.Contains(ServerPacketId.PlayerEmoteUpdate)) {
                ExecuteServerPacketHandler(id, ServerPacketId.PlayerEmoteUpdate, packet.PlayerEmoteUpdate);
            }
        }

        /**
         * Executes the correct packet handler corresponding to this packet.
         * Assumes that the packet is not read yet.
         */
        private void ExecuteClientPacketHandler(ClientPacketId packetId, IPacketData packetData) {
            //Logger.Info(this, $"recieve pkid client: {packetId}");

            if (!_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Warn(this, $"There is no client packet handler registered for ID: {packetId}");
                return;
            }

            // Invoke the packet handler for this ID on the Unity main thread
            ThreadUtil.RunActionOnMainThread(() => {
                try {
                    _clientPacketHandlers[packetId].Invoke(packetData);
                } catch (Exception e) {
                    Logger.Error(this, $"Exception occured while executing client packet handler for packet ID: {packetId}, message: {e.Message}, stacktrace: {e.StackTrace}");
                }
            });
        }
        
        /**
         * Executes the correct packet handler corresponding to this packet.
         * Assumes that the packet is not read yet.
         */
        private void ExecuteServerPacketHandler(ushort id, ServerPacketId packetId, IPacketData packetData) {
            //Logger.Info(this, $"recieve pkid server: {packetId}");

            if (!_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Warn(this, $"There is no server packet handler registered for ID: {packetId}");
                return;
            }

            // Invoke the packet handler for this ID directly, in contrast to the client packet handling.
            // We don't do anything game specific with server packet handler, so there's no need to do it
            // on the Unity main thread
            try {
                _serverPacketHandlers[packetId].Invoke(id, packetData);
            } catch (Exception e) {
                Logger.Error(this, $"Exception occured while executing server packet handler for packet ID: {packetId}, message: {e.Message}, stacktrace: {e.StackTrace}");
            }
        }

        public void RegisterClientPacketHandler<T>(ClientPacketId packetId, GenericClientPacketHandler<T> packetHandler) where T : IPacketData {
            if (_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to register already existing client packet handler: {packetId}");
                return;
            }

            // We can't store these kinds of generic delegates in a dictionary,
            // so we wrap it in a function that casts it
            _clientPacketHandlers[packetId] = iPacket => {
                packetHandler((T) iPacket);
            };
        }

        public void RegisterClientPacketHandler(ClientPacketId packetId, Action handler) {
            if (_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to register already existing client packet handler: {packetId}");
                return;
            }

            _clientPacketHandlers[packetId] = iPacket => {
                handler();
            };
        }

        public void DeregisterClientPacketHandler(ClientPacketId packetId) {
            if (!_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to remove non-existent client packet handler: {packetId}");
                return;
            }

            _clientPacketHandlers.Remove(packetId);
        }
        
        public void RegisterServerPacketHandler<T>(ServerPacketId packetId, GenericServerPacketHandler<T> packetHandler) where T : IPacketData {
            if (_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to register already existing server packet handler: {packetId}");
                return;
            }

            // We can't store these kinds of generic delegates in a dictionary,
            // so we wrap it in a function that casts it
            _serverPacketHandlers[packetId] = (id, iPacket) => {
                packetHandler(id, (T) iPacket);
            };
        }

        public void RegisterServerPacketHandler(ServerPacketId packetId, EmptyServerPacketHandler handler) {
            if (_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to register already existing client packet handler: {packetId}");
                return;
            }

            _serverPacketHandlers[packetId] = (id, iPacket) => {
                handler(id);
            };
        }

        public void DeregisterServerPacketHandler(ServerPacketId packetId) {
            if (!_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to remove non-existent server packet handler: {packetId}");
                return;
            }

            _serverPacketHandlers.Remove(packetId);
        }

        public static List<Packet> HandleReceivedData(byte[] receivedData, ref byte[] leftoverData) {
            var currentData = receivedData;
            
            // Check whether we have leftover data from the previous read, and concatenate the two byte arrays
            if (leftoverData != null && leftoverData.Length > 0) {
                currentData = new byte[leftoverData.Length + receivedData.Length];

                // Copy over the leftover data into the current data array
                for (var i = 0; i < leftoverData.Length; i++) {
                    currentData[i] = leftoverData[i];
                }

                // Copy over the trimmed data into the current data array
                for (var i = 0; i < receivedData.Length; i++) {
                    currentData[leftoverData.Length + i] = receivedData[i];
                }

                leftoverData = null;
            }

            // Create packets from the data
            return ByteArrayToPackets(currentData, ref leftoverData);
        }

        private static List<Packet> ByteArrayToPackets(byte[] data, ref byte[] leftover) {
            var packets = new List<Packet>();

            // Keep track of current index in the data array
            var readIndex = 0;
            
            // The only break from this loop is when there is no new packet to be read
            do {
                // If there is still an int (4 bytes) to read in the data,
                // it represents the next packet's length
                var packetLength = 0;
                var unreadDataLength = data.Length - readIndex;
                if (unreadDataLength > 1) {
                    packetLength = BitConverter.ToUInt16(data, readIndex);
                    readIndex += 2;
                }

                // There is no new packet, so we can break
                if (packetLength <= 0) {
                    break;
                }
                
                // Check whether our given data array actually contains
                // the same number of bytes as the packet length
                if (data.Length - readIndex < packetLength) {
                    // There is not enough bytes in the data array to fill the requested packet with
                    // So we put everything, including the packet length ushort (2 bytes) into the leftover byte array
                    leftover = new byte[unreadDataLength];
                    for (var i = 0; i < unreadDataLength; i++) {
                        // Make sure to index data 2 bytes earlier, since we incremented
                        // when we read the packet length ushort
                        leftover[i] = data[readIndex - 2 + i];
                    }

                    break;
                }

                // Read the next packet's length in bytes
                var packetData = new byte[packetLength];
                for (var i = 0; i < packetLength; i++) {
                    packetData[i] = data[readIndex + i];
                }

                readIndex += packetLength;
                
                // Create a packet out of this byte array
                var newPacket = new Packet(packetData);
                
                // Add it to the list of parsed packets
                packets.Add(newPacket);
            } while (true);
            
            return packets;
        }
    }
}