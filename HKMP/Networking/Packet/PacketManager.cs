using System;
using System.Collections.Generic;
using HKMP.Networking.Packet.Custom;
using HKMP.Networking.Packet.Custom.Update;
using HKMP.Util;

namespace HKMP.Networking.Packet {
    public delegate void ClientPacketHandler(IPacket packet);
    public delegate void GenericClientPacketHandler<in T>(T packet) where T : IPacket;
    public delegate void ServerPacketHandler(ushort id, IPacket packet);
    public delegate void GenericServerPacketHandler<in T>(ushort id, T packet) where T : IPacket;
    
    /**
     * Manages incoming packets by executing a corresponding registered handler
     */
    public class PacketManager {

        // Handlers that deal with data from the server intended for the client
        private readonly Dictionary<PacketId, ClientPacketHandler> _clientPacketHandlers;
        // Handlers that deal with data from the client intended for the server
        private readonly Dictionary<PacketId, ServerPacketHandler> _serverPacketHandlers;

        /**
         * Manages packets that are received by the given NetClient
         */
        public PacketManager() {
            _clientPacketHandlers = new Dictionary<PacketId, ClientPacketHandler>();
            _serverPacketHandlers = new Dictionary<PacketId, ServerPacketHandler>();
        }

        /**
         * Handle data received by a client
         */
        public void HandleClientPackets(List<Packet> packets) {
            // Execute corresponding packet handlers
            foreach (var packet in packets) {
                ExecuteClientPacketHandler(packet);
            }
        }

        /**
         * Handle data received by the server
         */
        public void HandleServerPackets(ushort id, List<Packet> packets) {
            // Execute corresponding packet handlers
            foreach (var packet in packets) {
                ExecuteServerPacketHandler(id, packet);
            }
        }

        /**
         * Executes the correct packet handler corresponding to this packet.
         * Assumes that the packet is not read yet.
         */
        private void ExecuteClientPacketHandler(Packet packet) {
            var packetId = packet.ReadPacketId();

            if (!_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Warn(this, $"There is no client packet handler registered for ID: {packetId}");
                return;
            }

            var instantiatedPacket = InstantiateClientPacket(packetId, packet);
            
            if (instantiatedPacket == null) {
                Logger.Warn(this, $"Could not instantiate client packet with ID: {packetId}");
                return;
            }
            
            // Read the packet data into the packet object before sending it to the packet handler
            instantiatedPacket.ReadPacket();

            // Invoke the packet handler for this ID on the Unity main thread
            ThreadUtil.RunActionOnMainThread(() => {
                try {
                    _clientPacketHandlers[packetId].Invoke(instantiatedPacket);
                } catch (Exception e) {
                    Logger.Error(this, $"Exception occured while executing client packet handler for packet ID: {packetId}, message: {e.Message}, stacktrace: {e.StackTrace}");
                }
            });
        }
        
        /**
         * Executes the correct packet handler corresponding to this packet.
         * Assumes that the packet is not read yet.
         */
        private void ExecuteServerPacketHandler(ushort id, Packet packet) {
            var packetId = packet.ReadPacketId();

            if (!_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Warn(this, $"There is no server packet handler registered for ID: {packetId}");
                return;
            }
            
            var instantiatedPacket = InstantiateServerPacket(packetId, packet);
            
            if (instantiatedPacket == null) {
                Logger.Warn(this, $"Could not instantiate server packet with ID: {packetId}");
                return;
            }

            // Read the packet data into the packet object before sending it to the packet handler
            instantiatedPacket.ReadPacket();
            
            // Invoke the packet handler for this ID directly, in contrast to the client packet handling.
            // We don't do anything game specific with server packet handler, so there's no need to do it
            // on the Unity main thread
            try {
                _serverPacketHandlers[packetId].Invoke(id, instantiatedPacket);
            } catch (Exception e) {
                Logger.Error(this, $"Exception occured while executing server packet handler for packet ID: {packetId}, message: {e.Message}, stacktrace: {e.StackTrace}");
            }
        }

        public void RegisterClientPacketHandler<T>(PacketId packetId, GenericClientPacketHandler<T> packetHandler) where T : IPacket {
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

        public void DeregisterClientPacketHandler(PacketId packetId) {
            if (!_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to remove non-existent client packet handler: {packetId}");
                return;
            }

            _clientPacketHandlers.Remove(packetId);
        }
        
        public void RegisterServerPacketHandler<T>(PacketId packetId, GenericServerPacketHandler<T> packetHandler) where T : IPacket {
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

        public void DeregisterServerPacketHandler(PacketId packetId) {
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
                    if (unreadDataLength >= 4) {
                        packetLength = BitConverter.ToInt32(data, readIndex);
                        readIndex += 4;
                    } else {
                        // There was data to be read, but not an entire int
                        // So we put the leftover data into the reference byte array
                        leftover = new byte[unreadDataLength];
                        for (var i = 0; i < unreadDataLength; i++) {
                            leftover[i] = data[readIndex + i];
                        }
                    }
                }

                // There is no new packet, so we can break
                if (packetLength <= 0) {
                    break;
                }
                
                // Check whether our given data array actually contains
                // the same number of bytes as the packet length
                if (data.Length - readIndex < packetLength) {
                    // There is not enough bytes in the data array to fill the requested packet with
                    // So we put everything, including the packet length int (4 bytes) into the leftover byte array
                    leftover = new byte[unreadDataLength];
                    for (var i = 0; i < unreadDataLength; i++) {
                        // Make sure to index data 4 bytes earlier, since we incremented
                        // when we read the packet length int
                        leftover[i] = data[readIndex - 4 + i];
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

        /**
         * We somehow need to instantiate the correct implementation of the
         * IPacket, so we do it here
         */
        private IPacket InstantiateClientPacket(PacketId packetId, Packet packet) {
            switch (packetId) {
                case PacketId.PlayerConnect:
                    return new ClientPlayerConnectPacket(packet);
                case PacketId.PlayerDisconnect:
                    return new ClientPlayerDisconnectPacket(packet);
                case PacketId.ServerShutdown:
                    return new ServerShutdownPacket(packet);
                case PacketId.AlreadyInScene:
                    return new ClientAlreadyInScenePacket(packet);
                case PacketId.PlayerEnterScene:
                    return new ClientPlayerEnterScenePacket(packet);
                case PacketId.PlayerLeaveScene:
                    return new ClientPlayerLeaveScenePacket(packet);
                case PacketId.PlayerUpdate:
                    return new ClientUpdatePacket(packet);
                case PacketId.PlayerDeath:
                    return new ClientPlayerDeathPacket(packet);
                case PacketId.PlayerTeamUpdate:
                    return new ClientPlayerTeamUpdatePacket(packet);
                case PacketId.GameSettingsUpdated:
                    return new GameSettingsUpdatePacket(packet);
                case PacketId.DreamshieldSpawn:
                    return new ClientDreamshieldSpawnPacket(packet);
                case PacketId.DreamshieldDespawn:
                    return new ClientDreamshieldDespawnPacket(packet);
                case PacketId.DreamshieldUpdate:
                    return new ClientDreamshieldUpdatePacket(packet);
                default:
                    return null;
            }
        }
        
        private IPacket InstantiateServerPacket(PacketId packetId, Packet packet) {
            switch (packetId) {
                case PacketId.HelloServer:
                    return new HelloServerPacket(packet);
                case PacketId.PlayerDisconnect:
                    return new ServerPlayerDisconnectPacket(packet);
                case PacketId.PlayerEnterScene:
                    return new ServerPlayerEnterScenePacket(packet);
                case PacketId.PlayerLeaveScene:
                    return new ServerPlayerLeaveScenePacket(packet);
                case PacketId.PlayerUpdate:
                    return new ServerUpdatePacket(packet);
                case PacketId.PlayerDeath:
                    return new ServerPlayerDeathPacket(packet);
                case PacketId.PlayerTeamUpdate:
                    return new ServerPlayerTeamUpdatePacket(packet);
                case PacketId.GameSettingsUpdated:
                    return new GameSettingsUpdatePacket(packet);
                case PacketId.DreamshieldSpawn:
                    return new ServerDreamshieldSpawnPacket(packet);
                case PacketId.DreamshieldDespawn:
                    return new ServerDreamshieldDespawnPacket(packet);
                case PacketId.DreamshieldUpdate:
                    return new ServerDreamshieldUpdatePacket(packet);
                default:
                    return null;
            }
        }
        
    }
}