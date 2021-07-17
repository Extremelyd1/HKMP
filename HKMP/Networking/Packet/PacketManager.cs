using System;
using Hkmp.Util;

namespace Hkmp.Networking.Packet {
    public partial class PacketManager {
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

            if (packet.DataPacketIds.Contains(ClientPacketId.AlreadyInScene)) {
                ExecuteClientPacketHandler(ClientPacketId.AlreadyInScene, packet.AlreadyInScene);
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