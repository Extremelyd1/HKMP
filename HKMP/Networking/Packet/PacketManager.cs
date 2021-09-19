using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;

namespace Hkmp.Networking.Packet {
    public delegate void ClientPacketHandler(IPacketData packet);

    public delegate void GenericClientPacketHandler<in T>(T packet) where T : IPacketData;

    public delegate void EmptyServerPacketHandler(ushort id);

    public delegate void ServerPacketHandler(ushort id, IPacketData packet);

    public delegate void GenericServerPacketHandler<in T>(ushort id, T packet) where T : IPacketData;
    
    /**
     * Manages packets that are received by the given NetClient
     */
    public class PacketManager {
        // Handlers that deal with data from the server intended for the client
        private readonly Dictionary<ClientPacketId, ClientPacketHandler> _clientPacketHandlers;

        // Handlers that deal with data from the client intended for the server
        private readonly Dictionary<ServerPacketId, ServerPacketHandler> _serverPacketHandlers;
        
        public PacketManager() {
            _clientPacketHandlers = new Dictionary<ClientPacketId, ClientPacketHandler>();
            _serverPacketHandlers = new Dictionary<ServerPacketId, ServerPacketHandler>();
        }
    
        /**
         * Handle data received by a client
         */
        public void HandleClientPacket(ClientUpdatePacket packet) {
            // Execute corresponding packet handlers
            foreach (var idPacketDataPair in packet.GetPacketData()) {
                var packetId = idPacketDataPair.Key;
                var packetData = idPacketDataPair.Value;

                // Check if this is a collection and if so, execute the handler for each instance in it
                if (packetData is RawPacketDataCollection rawPacketDataCollection) {
                    foreach (var dataInstance in rawPacketDataCollection.DataInstances) {
                        ExecuteClientPacketHandler(packetId, dataInstance);
                    }
                } else {
                    ExecuteClientPacketHandler(packetId, packetData);
                }
            }
        }

        /**
         * Executes the correct packet handler corresponding to this packet.
         * Assumes that the packet is not read yet.
         */
        private void ExecuteClientPacketHandler(ClientPacketId packetId, IPacketData packetData) {
            if (!_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Get().Warn(this, $"There is no client packet handler registered for ID: {packetId}");
                return;
            }

            // Invoke the packet handler for this ID on the Unity main thread
            ThreadUtil.RunActionOnMainThread(() => {
                try {
                    _clientPacketHandlers[packetId].Invoke(packetData);
                } catch (Exception e) {
                    Logger.Get().Error(this,
                        $"Exception occured while executing client packet handler for packet ID: {packetId}, message: {e.Message}, stacktrace: {e.StackTrace}");
                }
            });
        }
        
        /**
         * Handle data received by the server
         */
        public void HandleServerPacket(ushort id, ServerUpdatePacket packet) {
            // Execute corresponding packet handlers
            foreach (var idPacketDataPair in packet.GetPacketData()) {
                var packetId = idPacketDataPair.Key;
                var packetData = idPacketDataPair.Value;

                // Check if this is a collection and if so, execute the handler for each instance in it
                if (packetData is RawPacketDataCollection rawPacketDataCollection) {
                    foreach (var dataInstance in rawPacketDataCollection.DataInstances) {
                        ExecuteServerPacketHandler(id, packetId, dataInstance);
                    }
                } else {
                    ExecuteServerPacketHandler(id, packetId, packetData);
                }
            }
        }

        /**
         * Executes the correct packet handler corresponding to this packet.
         * Assumes that the packet is not read yet.
         */
        private void ExecuteServerPacketHandler(ushort id, ServerPacketId packetId, IPacketData packetData) {
            if (!_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Get().Warn(this, $"There is no server packet handler registered for ID: {packetId}");
                return;
            }

            // Invoke the packet handler for this ID directly, in contrast to the client packet handling.
            // We don't do anything game specific with server packet handler, so there's no need to do it
            // on the Unity main thread
            try {
                _serverPacketHandlers[packetId].Invoke(id, packetData);
            } catch (Exception e) {
                Logger.Get().Error(this,
                    $"Exception occured while executing server packet handler for packet ID: {packetId}, message: {e.Message}, stacktrace: {e.StackTrace}");
            }
        }

        public void RegisterClientPacketHandler<T>(ClientPacketId packetId, GenericClientPacketHandler<T> packetHandler)
            where T : IPacketData {
            if (_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Get().Error(this, $"Tried to register already existing client packet handler: {packetId}");
                return;
            }

            // We can't store these kinds of generic delegates in a dictionary,
            // so we wrap it in a function that casts it
            _clientPacketHandlers[packetId] = iPacket => { packetHandler((T) iPacket); };
        }

        public void RegisterClientPacketHandler(ClientPacketId packetId, Action handler) {
            if (_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Get().Error(this, $"Tried to register already existing client packet handler: {packetId}");
                return;
            }

            _clientPacketHandlers[packetId] = iPacket => { handler(); };
        }

        public void DeregisterClientPacketHandler(ClientPacketId packetId) {
            if (!_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Get().Error(this, $"Tried to remove non-existent client packet handler: {packetId}");
                return;
            }

            _clientPacketHandlers.Remove(packetId);
        }

        public void RegisterServerPacketHandler<T>(ServerPacketId packetId, GenericServerPacketHandler<T> packetHandler)
            where T : IPacketData {
            if (_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Get().Error(this, $"Tried to register already existing server packet handler: {packetId}");
                return;
            }

            // We can't store these kinds of generic delegates in a dictionary,
            // so we wrap it in a function that casts it
            _serverPacketHandlers[packetId] = (id, iPacket) => { packetHandler(id, (T) iPacket); };
        }

        public void RegisterServerPacketHandler(ServerPacketId packetId, EmptyServerPacketHandler handler) {
            if (_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Get().Error(this, $"Tried to register already existing client packet handler: {packetId}");
                return;
            }

            _serverPacketHandlers[packetId] = (id, iPacket) => { handler(id); };
        }

        public void DeregisterServerPacketHandler(ServerPacketId packetId) {
            if (!_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Get().Error(this, $"Tried to remove non-existent server packet handler: {packetId}");
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