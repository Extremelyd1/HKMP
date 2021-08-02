using System;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;

namespace Hkmp.Networking.Packet {
    public partial class PacketManager {
        /**
         * Handle data received by a client
         */
        public void HandleClientPacket(ClientUpdatePacket packet) {
            // Execute corresponding packet handlers
            foreach (var idPacketDataPair in packet.PacketData) {
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

            // TODO: figure out how to make sure this fires on the Unity main thread
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
    }
}