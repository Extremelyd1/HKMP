using System.Collections.Generic;
using HKMP.Networking;
using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Game {
    /**
     * Class that manages the server state (similar to ClientManager).
     * For example the current scene of each player, to prevent sending redundant traffic.
     */
    public class ServerManager {

        private readonly NetworkManager _networkManager;

        private readonly Dictionary<int, string> _clientScenes;
        private readonly Dictionary<int, Vector3> _lastPositions;

        public ServerManager(NetworkManager networkManager, PacketManager packetManager) {
            _networkManager = networkManager;

            _clientScenes = new Dictionary<int, string>();
            _lastPositions = new Dictionary<int, Vector3>();
            
            // Register packet handlers
            packetManager.RegisterServerPacketHandler(PacketId.HelloServer, OnHelloServer);
            packetManager.RegisterServerPacketHandler(PacketId.SceneChange, OnClientChangeScene);
            packetManager.RegisterServerPacketHandler(PacketId.PlayerPositionUpdate, OnPlayerUpdatePosition);
            packetManager.RegisterServerPacketHandler(PacketId.Disconnect, OnPlayerDisconnect);
        }

        private void OnHelloServer(int id, Packet packet) {
            Logger.Info(this, $"Received Hello packet from ID {id}");
            
            // Read data from packet
            var sceneName = packet.ReadString();
            var position = packet.ReadVector3();

            // If scene name is NonGameplay, the client is not in a gameplay scene,
            // so there is nothing to send to other clients
            if (sceneName.Equals("NonGameplay")) {
                return;
            }
            
            // Store scene of player in mapping
            _clientScenes[id] = sceneName;
            // Store position of player in mapping
            _lastPositions[id] = position;

            // TODO: check whether we need to send the position update already
            // It might arrive earlier than the enter scene packet due to TCP/UDP, thus having no impact 
            // Moreover, we don't do this with the scene change packet either
            
            // Create PlayerEnterScene packet
            var enterScenePacket = new Packet(PacketId.PlayerEnterScene);
            enterScenePacket.Write(id);
            enterScenePacket.Write(position);

            // Send the packets to all clients in the same scene except the source client
            foreach (var idScenePair in _clientScenes) {
                if (idScenePair.Key != id && idScenePair.Value.Equals(sceneName)) {
                    _networkManager.GetNetServer().SendTcp(idScenePair.Key, enterScenePacket);
                    
                    // Also send the source client a packet that this player is in their scene
                    var alreadyInScenePacket = new Packet(PacketId.PlayerEnterScene);
                    alreadyInScenePacket.Write(idScenePair.Key);
                    alreadyInScenePacket.Write(_lastPositions[idScenePair.Key]);

                    _networkManager.GetNetServer().SendTcp(id, alreadyInScenePacket);
                }
            }
        }
        
        private void OnClientChangeScene(int id, Packet packet) {
            // Initialize with default value, override if mapping has key
            var oldSceneName = "NonGameplay";
            if (_clientScenes.ContainsKey(id)) {
                oldSceneName = _clientScenes[id];                
            }
            
            var newSceneName = packet.ReadString();
            
            // Sanity check: whether the scene has actually changed
            if (oldSceneName.Equals(newSceneName)) {
                Logger.Warn(this, $"Received SceneChange packet, but scenes did not change for ID {id}");
                return;
            }
            
            Logger.Info(this, $"Received SceneChange packet from ID {id}, from {oldSceneName} to {newSceneName}");

            // Read the position in the new scene
            var position = packet.ReadVector3();
            
            // Store it in the position mapping
            _lastPositions[id] = position;
            
            // Create packets in advance
            // Create a PlayerLeaveScene packet containing the ID
            // of the player leaving the scene
            var leaveScenePacket = new Packet(PacketId.PlayerLeaveScene);
            leaveScenePacket.Write(id);
            
            // Create a PlayerEnterScene packet containing the ID
            // of the player entering the scene and their position
            var enterScenePacket = new Packet(PacketId.PlayerEnterScene);
            enterScenePacket.Write(id);
            enterScenePacket.Write(position);
            
            foreach (var idScenePair in _clientScenes) {
                // Send the packet to all clients on the old scene
                // to indicate that this client has left their scene
                if (idScenePair.Key != id && idScenePair.Value.Equals(oldSceneName)) {
                    Logger.Info(this, $"Sending leave scene packet to {idScenePair.Key}");
                    _networkManager.GetNetServer().SendTcp(idScenePair.Key, leaveScenePacket);
                }
                
                // Send the packet to all clients on the new scene
                // to indicate that this client has entered their scene
                if (idScenePair.Key != id && idScenePair.Value.Equals(newSceneName)) {
                    Logger.Info(this, $"Sending enter scene packet to {idScenePair.Key}");
                    _networkManager.GetNetServer().SendTcp(idScenePair.Key, enterScenePacket);
                    
                    Logger.Info(this, $"Sending that {idScenePair.Key} is already in scene to {id}");
                    
                    // Also send a packet to the client that switched scenes,
                    // notifying that these players are already in this new scene
                    var alreadyInScenePacket = new Packet(PacketId.PlayerEnterScene);
                    alreadyInScenePacket.Write(idScenePair.Key);
                    alreadyInScenePacket.Write(_lastPositions[idScenePair.Key]);
                    
                    _networkManager.GetNetServer().SendTcp(id, alreadyInScenePacket);
                }
            }
            
            // Store the new scene in the mapping
            _clientScenes[id] = newSceneName;
        }

        private void OnPlayerUpdatePosition(int id, Packet packet) {
            if (!_clientScenes.ContainsKey(id)) {
                Logger.Warn(this, $"Received PlayerPositionUpdate packet, but player with ID {id} is not in mapping");
                return;
            }
            
            // Get current scene of player
            var currentScene = _clientScenes[id];
            
            var newPosition = packet.ReadVector3();
            
            // Store the new position in the last position mapping
            _lastPositions[id] = newPosition;
            
            // Create the packet in advance
            var positionUpdatePacket = new Packet(PacketId.PlayerPositionUpdate);
            positionUpdatePacket.Write(id);
            positionUpdatePacket.Write(newPosition);

            // Send the packet to all clients in the same scene
            foreach (var idScenePair in _clientScenes) {
                if (idScenePair.Key != id && idScenePair.Value.Equals(currentScene)) {
                    _networkManager.GetNetServer().SendUdp(idScenePair.Key, positionUpdatePacket);
                }
            }
        }

        private void OnPlayerDisconnect(int id, Packet packet) {
            // Always propagate this packet to the NetServer
            _networkManager.GetNetServer().OnClientDisconnect(id);
            
            if (!_clientScenes.ContainsKey(id)) {
                Logger.Warn(this,$"Received Disconnect packet, but player with ID {id} is not in mapping");
                return;
            }

            Logger.Info(this, $"Received Disconnect packet from ID {id}");

            // Get the scene that client was in while disconnecting
            var currentScene = _clientScenes[id];
            
            // Create a PlayerLeaveScene packet containing the ID
            // of the player disconnecting
            var leaveScenePacket = new Packet(PacketId.PlayerLeaveScene);
            leaveScenePacket.Write(id);

            // Send the packet to all clients in the same scene
            foreach (var idScenePair in _clientScenes) {
                if (idScenePair.Key != id && idScenePair.Value.Equals(currentScene)) {
                    _networkManager.GetNetServer().SendTcp(idScenePair.Key, leaveScenePacket);
                }
            }
        }

    }
}