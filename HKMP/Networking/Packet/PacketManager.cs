using System.Collections.Generic;
using HKMP.Util;

namespace HKMP.Networking.Packet {
    public delegate void ClientPacketHandler(Packet packet);
    public delegate void ServerPacketHandler(int id, Packet packet);
    
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
        public void HandleClientData(byte[] data) {
            // Transform raw data into packets
            var packets = ByteArrayToPackets(data);
            // Execute corresponding packet handlers
            foreach (var packet in packets) {
                ExecuteClientPacketHandler(packet);
            }
        }

        /**
         * Handle data received by the server
         */
        public void HandleServerData(int id, byte[] data) {
            // Transform raw data into packets
            var packets = ByteArrayToPackets(data);
            
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
            var packetId = (PacketId) packet.ReadInt();

            if (!_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Warn(this, $"There is no packet handler registered for ID: {packetId}");
                return;
            }

            // Invoke the packet handler for this ID on the Unity main thread
            ThreadUtil.RunActionOnMainThread(() => {
                _clientPacketHandlers[packetId].Invoke(packet);
            });
        }
        
        /**
         * Executes the correct packet handler corresponding to this packet.
         * Assumes that the packet is not read yet.
         */
        private void ExecuteServerPacketHandler(int id, Packet packet) {
            var packetId = (PacketId) packet.ReadInt();

            if (!_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Warn(this, $"There is no packet handler registered for ID: {packetId}");
                return;
            }

            // Invoke the packet handler for this ID on the Unity main thread
            ThreadUtil.RunActionOnMainThread(() => {
                _serverPacketHandlers[packetId].Invoke(id, packet);
            });
        }

        public void RegisterClientPacketHandler(PacketId packetId, ClientPacketHandler packetHandler) {
            if (_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to register already existing client packet handler: {packetId}");
                return;
            }

            _clientPacketHandlers[packetId] = packetHandler;
        }

        public void DeregisterClientPacketHandler(PacketId packetId) {
            if (!_clientPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to remove non-existent client packet handler: {packetId}");
                return;
            }

            _clientPacketHandlers.Remove(packetId);
        }
        
        public void RegisterServerPacketHandler(PacketId packetId, ServerPacketHandler packetHandler) {
            if (_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to register already existing server packet handler: {packetId}");
                return;
            }

            _serverPacketHandlers[packetId] = packetHandler;
        }

        public void DeregisterServerPacketHandler(PacketId packetId) {
            if (!_serverPacketHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to remove non-existent server packet handler: {packetId}");
                return;
            }

            _serverPacketHandlers.Remove(packetId);
        }

        private List<Packet> ByteArrayToPackets(byte[] data) {
            var packets = new List<Packet>();
            
            var dataAsPacket = new Packet(data);
            
            // The only break from this loop is when there is no new packet to be read
            do {
                // If there is still an int to read in the data,
                // it represents the next packet's length
                var packetLength = 0;
                if (dataAsPacket.UnreadLength() >= 4) {
                    packetLength = dataAsPacket.ReadInt();
                }
                
                // There is no new packet, so we can break
                if (packetLength <= 0) {
                    break;
                }

                // Read the next packet's length in bytes
                var packetData = dataAsPacket.ReadBytes(packetLength);
                
                // Create a packet out of this byte array
                var newPacket = new Packet(packetData);
                
                // Add it to the list of parsed packets
                packets.Add(newPacket);
            } while (true);
            
            return packets;
        }
        
    }
}