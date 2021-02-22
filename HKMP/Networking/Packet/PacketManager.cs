using System.Collections.Generic;
using HKMP.Networking.Client;

namespace HKMP.Networking.Packet {
    public delegate void PacketHandler(Packet packet);
    
    public class PacketManager {

        private readonly Dictionary<PacketId, PacketHandler> _packetHandlers;

        /**
         * Manages packets that are received by the given NetClient
         */
        public PacketManager(NetClient netClient) {
            _packetHandlers = new Dictionary<PacketId, PacketHandler>();

            netClient.RegisterOnReceive(OnReceiveData);
        }

        private bool OnReceiveData(byte[] data) {
            var packets = ByteArrayToPackets(data);
            
            return true;
        }

        /**
         * Executes the correct packet handler corresponding to this packet.
         * Assumes that the packet is not read yet.
         */
        private void ExecutePacketHandler(Packet packet) {
            var packetId = (PacketId) packet.ReadInt();

            if (!_packetHandlers.ContainsKey(packetId)) {
                Logger.Warn(this, $"There is no packet handler registered for ID: {packetId}");
                return;
            }

            // Invoke the packet handler for this ID
            _packetHandlers[packetId].Invoke(packet);
        }

        public void RegisterPacketHandler(PacketId packetId, PacketHandler packetHandler) {
            if (_packetHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to register already existing key: {packetId}");
                return;
            }

            _packetHandlers.Add(packetId, packetHandler);
        }

        public void DeregisterPacketHandler(PacketId packetId) {
            if (!_packetHandlers.ContainsKey(packetId)) {
                Logger.Error(this, $"Tried to remove non-existent packet handler: {packetId}");
                return;
            }

            _packetHandlers.Remove(packetId);
        }

        private List<Packet> ByteArrayToPackets(byte[] data) {
            var packets = new List<Packet>();

            var dataAsPacket = new Packet(data);

            // The only break from this loop is when there is no new packet to be read
            do {
                // If there is still an int to read in the data,
                // it represents the next packet's length
                int packetLength = 0;
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