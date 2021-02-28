using System.Collections.Generic;
using HKMP.Networking;
using HKMP.Networking.Packet;
using Modding;
using UnityEngine;

namespace HKMP.Game.Server {
    /**
     * Class that manages the server state (similar to ClientManager).
     * For example the current scene of each player, to prevent sending redundant traffic.
     */
    public class ServerManager {
        // TODO: switch to a system where each different packet has their own class
        // so that we can write/read entire PlayerData objects to/from the packets
        
        // TODO: decide whether it is better to always transmit entire PlayerData objects instead of
        // multiple packets (one for position, one for scale, one for animation, etc.)

        private readonly NetworkManager _networkManager;

        private readonly Dictionary<int, PlayerData> _playerData;

        public ServerManager(NetworkManager networkManager, PacketManager packetManager) {
            _networkManager = networkManager;

            _playerData = new Dictionary<int, PlayerData>();
            
            // Register packet handlers
            packetManager.RegisterServerPacketHandler(PacketId.HelloServer, OnHelloServer);
            packetManager.RegisterServerPacketHandler(PacketId.SceneChange, OnClientChangeScene);
            packetManager.RegisterServerPacketHandler(PacketId.PlayerPositionUpdate, OnPlayerUpdatePosition);
            packetManager.RegisterServerPacketHandler(PacketId.PlayerScaleUpdate, OnPlayerUpdateScale);
            packetManager.RegisterServerPacketHandler(PacketId.Disconnect, OnPlayerDisconnect);
            packetManager.RegisterServerPacketHandler(PacketId.PlayerAnimationUpdate, OnPlayerUpdateAnimation);
            packetManager.RegisterServerPacketHandler(PacketId.PlayerDeath, OnPlayerDeath);
            
            // Register server shutdown handler
            _networkManager.GetNetServer().RegisterOnShutdown(OnServerShutdown);
            
            // Register application quit handler
            ModHooks.Instance.ApplicationQuitHook += OnApplicationQuit;
        }

        private void OnHelloServer(int id, Packet packet) {
            Logger.Info(this, $"Received Hello packet from ID {id}");
            
            // Read scene name from packet
            var sceneName = packet.ReadString();

            // If scene name is NonGameplay, the client is not in a gameplay scene,
            // so there is nothing to send to other clients
            if (sceneName.Equals("NotActive")) {
                return;
            }
            
            // Read the rest of the data, since we know that we have it
            var position = packet.ReadVector3();
            var scale = packet.ReadVector3();
            var currentClip = packet.ReadString();
            
            // Create new player data object
            var playerData = new PlayerData(sceneName, position, scale, currentClip);
            // Store data in mapping
            _playerData[id] = playerData;

            // TODO: check whether we need to send the position update already
            // It might arrive earlier than the enter scene packet due to TCP/UDP, thus having no impact 
            // Moreover, we don't do this with the scene change packet either
            
            // Create PlayerEnterScene packet
            var enterScenePacket = new Packet(PacketId.PlayerEnterScene);
            enterScenePacket.Write(id);
            enterScenePacket.Write(position);
            enterScenePacket.Write(scale);
            enterScenePacket.Write(currentClip);

            // Send the packets to all clients in the same scene except the source client
            foreach (var idPlayerDataPair in _playerData) {
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                var otherPlayerData = idPlayerDataPair.Value;
                if (otherPlayerData.CurrentScene.Equals(sceneName)) {
                    _networkManager.GetNetServer().SendTcp(idPlayerDataPair.Key, enterScenePacket);
                    
                    // Also send the source client a packet that this player is in their scene
                    var alreadyInScenePacket = new Packet(PacketId.PlayerEnterScene);
                    alreadyInScenePacket.Write(idPlayerDataPair.Key);
                    alreadyInScenePacket.Write(otherPlayerData.LastPosition);
                    alreadyInScenePacket.Write(otherPlayerData.LastScale);
                    alreadyInScenePacket.Write(otherPlayerData.LastAnimationClip);

                    _networkManager.GetNetServer().SendTcp(id, alreadyInScenePacket);
                }
            }
        }
        
        private void OnClientChangeScene(int id, Packet packet) {
            // Initialize with default value, override if mapping has key
            var oldSceneName = "NonGameplay";
            if (_playerData.ContainsKey(id)) {
                oldSceneName = _playerData[id].CurrentScene;                
            }
            
            var newSceneName = packet.ReadString();
            
            // Check whether the scene has changed, it might not change if
            // a player died and respawned in the same scene
            if (oldSceneName.Equals(newSceneName)) {
                Logger.Warn(this, $"Received SceneChange packet from ID {id}, from and to {oldSceneName}, probably a Death event");
            } else {
                Logger.Info(this, $"Received SceneChange packet from ID {id}, from {oldSceneName} to {newSceneName}");
            }

            // Read the position and scale in the new scene
            var position = packet.ReadVector3();
            var scale = packet.ReadVector3();
            var animationClip = packet.ReadString();
            
            // Store it in their PlayerData object
            var playerData = _playerData[id];
            playerData.CurrentScene = newSceneName;
            playerData.LastPosition = position;
            playerData.LastScale = scale;
            playerData.LastAnimationClip = animationClip;
            
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
            enterScenePacket.Write(scale);
            enterScenePacket.Write(animationClip);
            
            foreach (var idPlayerDataPair in _playerData) {
                // Skip source player
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                var otherPlayerData = idPlayerDataPair.Value;
                
                // Send the packet to all clients on the old scene
                // to indicate that this client has left their scene
                if (otherPlayerData.CurrentScene.Equals(oldSceneName)) {
                    Logger.Info(this, $"Sending leave scene packet to {idPlayerDataPair.Key}");
                    _networkManager.GetNetServer().SendTcp(idPlayerDataPair.Key, leaveScenePacket);
                }
                
                // Send the packet to all clients on the new scene
                // to indicate that this client has entered their scene
                if (otherPlayerData.CurrentScene.Equals(newSceneName)) {
                    Logger.Info(this, $"Sending enter scene packet to {idPlayerDataPair.Key}");
                    _networkManager.GetNetServer().SendTcp(idPlayerDataPair.Key, enterScenePacket);
                    
                    Logger.Info(this, $"Sending that {idPlayerDataPair.Key} is already in scene to {id}");
                    
                    // Also send a packet to the client that switched scenes,
                    // notifying that these players are already in this new scene
                    var alreadyInScenePacket = new Packet(PacketId.PlayerEnterScene);
                    alreadyInScenePacket.Write(idPlayerDataPair.Key);
                    alreadyInScenePacket.Write(otherPlayerData.LastPosition);
                    alreadyInScenePacket.Write(otherPlayerData.LastScale);
                    alreadyInScenePacket.Write(otherPlayerData.LastAnimationClip);
                    
                    _networkManager.GetNetServer().SendTcp(id, alreadyInScenePacket);
                }
            }
            
            // Store the new PlayerData object in the mapping
            _playerData[id] = playerData;
        }

        private void OnPlayerUpdatePosition(int id, Packet packet) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"Received PlayerPositionUpdate packet, but player with ID {id} is not in mapping");
                return;
            }
            
            // Get current scene of player
            var currentScene = _playerData[id].CurrentScene;
            
            var newPosition = packet.ReadVector3();
            
            // Store the new position in the last position mapping
            _playerData[id].LastPosition = newPosition;
            
            // Create the packet in advance
            var positionUpdatePacket = new Packet(PacketId.PlayerPositionUpdate);
            positionUpdatePacket.Write(id);
            positionUpdatePacket.Write(newPosition);

            // Send the packet to all clients in the same scene
            SendPacketToClientsInSameScene(positionUpdatePacket, false, currentScene, id);
        }
        
        private void OnPlayerUpdateScale(int id, Packet packet) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"Received PlayerScaleUpdate packet, but player with ID {id} is not in mapping");
                return;
            }
            
            // Get current scene of player
            var currentScene = _playerData[id].CurrentScene;
            
            var newScale = packet.ReadVector3();
            
            // Store the new position in the player data
            _playerData[id].LastScale = newScale;
            
            // Create the packet in advance
            var scaleUpdatePacket = new Packet(PacketId.PlayerScaleUpdate);
            scaleUpdatePacket.Write(id);
            scaleUpdatePacket.Write(newScale);

            // Send the packet to all clients in the same scene
            SendPacketToClientsInSameScene(scaleUpdatePacket, false, currentScene, id);
        }
        
        private void OnPlayerUpdateAnimation(int id, Packet packet) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"Received PlayerAnimationUpdate packet, but player with ID {id} is not in mapping");
                return;
            }
            
            // Get current scene of player
            var currentScene = _playerData[id].CurrentScene;
            
            // Get the clip name from the packet
            var clipName = packet.ReadString();
            
            // Store the new animation in the player data
            _playerData[id].LastAnimationClip = clipName;
            
            // Create the packet in advance
            var animationUpdatePacket = new Packet(PacketId.PlayerAnimationUpdate);
            animationUpdatePacket.Write(id);
            animationUpdatePacket.Write(clipName);
            
            // Fill the packet with all the other existing effect data
            var unreadLength = packet.UnreadLength();
            if (unreadLength > 0) {
                var leftoverData = packet.ReadBytes(unreadLength);
                animationUpdatePacket.Write(leftoverData);
            }

            // Send the packet to all clients in the same scene
            SendPacketToClientsInSameScene(animationUpdatePacket, false, currentScene, id);
        }

        private void OnPlayerDisconnect(int id, Packet packet) {
            // Always propagate this packet to the NetServer
            _networkManager.GetNetServer().OnClientDisconnect(id);

            if (!_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"Received Disconnect packet, but player with ID {id} is not in mapping");
                return;
            }

            Logger.Info(this, $"Received Disconnect packet from ID {id}");

            // Get the scene that client was in while disconnecting
            var currentScene = _playerData[id].CurrentScene;

            // Create a PlayerLeaveScene packet containing the ID
            // of the player disconnecting
            var leaveScenePacket = new Packet(PacketId.PlayerLeaveScene);
            leaveScenePacket.Write(id);

            // Send the packet to all clients in the same scene
            SendPacketToClientsInSameScene(leaveScenePacket, true, currentScene, id);
            
            // Now remove the client from the player data mapping
            _playerData.Remove(id);
        }

        private void OnPlayerDeath(int id, Packet packet) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"Received PlayerDeath packet, but player with ID {id} is not in mapping");
                return;
            }

            Logger.Info(this, $"Received PlayerDeath packet from ID {id}");
            
            // Get the scene that the client was last in
            var currentScene = _playerData[id].CurrentScene;
            
            // Create a new PlayerDeath packet containing the ID of the player that died
            var playerDeathPacket = new Packet(PacketId.PlayerDeath);
            playerDeathPacket.Write(id);
            
            // Send the packet to all clients in the same scene
            SendPacketToClientsInSameScene(playerDeathPacket, true, currentScene, id);
        }

        private void OnServerShutdown() {
            // Clear all existing player data
            _playerData.Clear();
        }

        private void OnApplicationQuit() {
            if (!_networkManager.GetNetServer().IsStarted) {
                return;
            }

            // Send a disconnect packet before exiting the application
            var shutdownPacket = new Packet(PacketId.Disconnect);
            _networkManager.GetNetServer().BroadcastTcp(shutdownPacket);
            _networkManager.StopServer();
        }

        private void SendPacketToClientsInSameScene(Packet packet, bool tcp, string targetScene, int excludeId) {
            foreach (var idScenePair in _playerData) {
                if (idScenePair.Key == excludeId) {
                    continue;
                }
                
                if (idScenePair.Value.CurrentScene.Equals(targetScene)) {
                    if (tcp) {
                        _networkManager.GetNetServer().SendTcp(idScenePair.Key, packet);   
                    } else {
                        _networkManager.GetNetServer().SendUdp(idScenePair.Key, packet);
                    }
                }
            }
        }

    }
}