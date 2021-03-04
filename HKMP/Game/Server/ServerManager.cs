using System.Collections.Generic;
using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
using HKMP.Networking.Server;
using Modding;
using UnityEngine;

namespace HKMP.Game.Server {
    /**
     * Class that manages the server state (similar to ClientManager).
     * For example the current scene of each player, to prevent sending redundant traffic.
     */
    public class ServerManager {
        // TODO: decide whether it is better to always transmit entire PlayerData objects instead of
        // multiple packets (one for position, one for scale, one for animation, etc.)

        private readonly NetServer _netServer;

        private readonly Game.Settings.GameSettings _gameSettings;

        private readonly Dictionary<int, PlayerData> _playerData;

        public ServerManager(NetworkManager networkManager, Game.Settings.GameSettings gameSettings, PacketManager packetManager) {
            _netServer = networkManager.GetNetServer();
            _gameSettings = gameSettings;

            _playerData = new Dictionary<int, PlayerData>();
            
            // Register packet handlers
            packetManager.RegisterServerPacketHandler<HelloServerPacket>(PacketId.HelloServer, OnHelloServer);
            packetManager.RegisterServerPacketHandler<PlayerChangeScenePacket>(PacketId.PlayerChangeScene, OnClientChangeScene);
            packetManager.RegisterServerPacketHandler<ServerPlayerPositionUpdatePacket>(PacketId.ServerPlayerPositionUpdate, OnPlayerUpdatePosition);
            packetManager.RegisterServerPacketHandler<ServerPlayerScaleUpdatePacket>(PacketId.ServerPlayerScaleUpdate, OnPlayerUpdateScale);
            packetManager.RegisterServerPacketHandler<PlayerDisconnectPacket>(PacketId.PlayerDisconnect, OnPlayerDisconnect);
            packetManager.RegisterServerPacketHandler<ServerPlayerAnimationUpdatePacket>(PacketId.ServerPlayerAnimationUpdate, OnPlayerUpdateAnimation);
            packetManager.RegisterServerPacketHandler<ServerPlayerDeathPacket>(PacketId.ServerPlayerDeath, OnPlayerDeath);
            
            // Register server shutdown handler
            _netServer.RegisterOnShutdown(OnServerShutdown);
            
            // Register application quit handler
            ModHooks.Instance.ApplicationQuitHook += OnApplicationQuit;
        }

        /**
         * Starts a server with the given port
         */
        public void Start(int port) {
            // Stop existing server
            if (_netServer.IsStarted) {
                Logger.Warn(this, "Server was running, shutting it down before starting");
                _netServer.Stop();
            }

            // Start server again with given port
            _netServer.Start(port);
        }

        /**
         * Stops the currently running server
         */
        public void Stop() {
            if (_netServer.IsStarted) {
                // Before shutting down, send TCP packets to all clients indicating
                // that the server is shutting down
                var shutdownPacket = new ServerShutdownPacket();
                shutdownPacket.CreatePacket();
                _netServer.BroadcastTcp(shutdownPacket);
                
                _netServer.Stop();
            } else {
                Logger.Warn(this, "Could not stop server, it was not started");
            }
        }

        /**
         * Called when the game settings are updated, and need to be broadcast
         */
        public void OnUpdateGameSettings() {
            var settingsUpdatePacket = new GameSettingsUpdatePacket {
                GameSettings = _gameSettings
            };
            settingsUpdatePacket.CreatePacket();
            
            _netServer.BroadcastTcp(settingsUpdatePacket);
        }

        private void OnHelloServer(int id, HelloServerPacket packet) {
            Logger.Info(this, $"Received Hello packet from ID {id}");
            
            // Start by sending the new client the current Server Settings
            var settingsUpdatePacket = new GameSettingsUpdatePacket {
                GameSettings = _gameSettings
            };
            settingsUpdatePacket.CreatePacket();
            
            _netServer.SendTcp(id, settingsUpdatePacket);
            
            // Read username from packet
            var username = packet.Username;

            // Read scene name from packet
            var sceneName = packet.SceneName;
            
            // Read the rest of the data, since we know that we have it
            var position = packet.Position;
            var scale = packet.Scale;
            var currentClip = packet.AnimationClipName;
            
            // Create new player data object
            var playerData = new PlayerData(username, sceneName, position, scale, currentClip);
            // Store data in mapping
            _playerData[id] = playerData;
            
            // TODO: check whether we need to send the position update already
            // It might arrive earlier than the enter scene packet due to TCP/UDP, thus having no impact 
            // Moreover, we don't do this with the scene change packet either
            
            // Create PlayerEnterScene packet
            var enterScenePacket = new PlayerEnterScenePacket {
                Id = id,
                Username = username,
                Position = position,
                Scale = scale,
                AnimationClipName = currentClip
            };
            enterScenePacket.CreatePacket();
            
            // Send the packets to all clients in the same scene except the source client
            foreach (var idPlayerDataPair in _playerData) {
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                var otherPlayerData = idPlayerDataPair.Value;
                if (otherPlayerData.CurrentScene.Equals(sceneName)) {
                    _netServer.SendTcp(idPlayerDataPair.Key, enterScenePacket);
                    
                    // Also send the source client a packet that this player is in their scene
                    var alreadyInScenePacket = new PlayerEnterScenePacket {
                        Id = idPlayerDataPair.Key,
                        Username = otherPlayerData.Name,
                        Position = otherPlayerData.LastPosition,
                        Scale = otherPlayerData.LastScale,
                        AnimationClipName = otherPlayerData.LastAnimationClip
                    };
                    alreadyInScenePacket.CreatePacket();
                    
                    _netServer.SendTcp(id, alreadyInScenePacket);
                }
            }
        }
        
        private void OnClientChangeScene(int id, PlayerChangeScenePacket packet) {
            // Initialize with default value, override if mapping has key
            var oldSceneName = "NonGameplay";
            if (_playerData.ContainsKey(id)) {
                oldSceneName = _playerData[id].CurrentScene;                
            }

            var newSceneName = packet.NewSceneName;
            
            // Check whether the scene has changed, it might not change if
            // a player died and respawned in the same scene
            if (oldSceneName.Equals(newSceneName)) {
                Logger.Warn(this, $"Received SceneChange packet from ID {id}, from and to {oldSceneName}, probably a Death event");
            } else {
                Logger.Info(this, $"Received SceneChange packet from ID {id}, from {oldSceneName} to {newSceneName}");
            }

            // Read the position and scale in the new scene
            var position = packet.Position;
            var scale = packet.Scale;
            var animationClipName = packet.AnimationClipName;
            
            // Store it in their PlayerData object
            var playerData = _playerData[id];
            playerData.CurrentScene = newSceneName;
            playerData.LastPosition = position;
            playerData.LastScale = scale;
            playerData.LastAnimationClip = animationClipName;
            
            // Create packets in advance
            // Create a PlayerLeaveScene packet containing the ID
            // of the player leaving the scene
            var leaveScenePacket = new PlayerLeaveScenePacket {
                Id = id
            };
            leaveScenePacket.CreatePacket();
            
            // Create a PlayerEnterScene packet containing the ID
            // of the player entering the scene and their position
            var enterScenePacket = new PlayerEnterScenePacket {
                Id = id,
                Username = playerData.Name,
                Position = position,
                Scale = scale,
                AnimationClipName = animationClipName
            };
            enterScenePacket.CreatePacket();
            
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
                    _netServer.SendTcp(idPlayerDataPair.Key, leaveScenePacket);
                }
                
                // Send the packet to all clients on the new scene
                // to indicate that this client has entered their scene
                if (otherPlayerData.CurrentScene.Equals(newSceneName)) {
                    Logger.Info(this, $"Sending enter scene packet to {idPlayerDataPair.Key}");
                    _netServer.SendTcp(idPlayerDataPair.Key, enterScenePacket);
                    
                    Logger.Info(this, $"Sending that {idPlayerDataPair.Key} is already in scene to {id}");
                    
                    // Also send a packet to the client that switched scenes,
                    // notifying that these players are already in this new scene
                    var alreadyInScenePacket = new PlayerEnterScenePacket {
                        Id = idPlayerDataPair.Key,
                        Username = otherPlayerData.Name,
                        Position = otherPlayerData.LastPosition,
                        Scale = otherPlayerData.LastScale,
                        AnimationClipName = otherPlayerData.LastAnimationClip
                    };
                    alreadyInScenePacket.CreatePacket();
                    
                    _netServer.SendTcp(id, alreadyInScenePacket);
                }
            }
            
            // Store the new PlayerData object in the mapping
            _playerData[id] = playerData;
        }

        private void OnPlayerUpdatePosition(int id, ServerPlayerPositionUpdatePacket packet) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"Received PlayerPositionUpdate packet, but player with ID {id} is not in mapping");
                return;
            }
            
            // Get current scene of player
            var currentScene = _playerData[id].CurrentScene;

            var newPosition = packet.Position;
            
            // Store the new position in the last position mapping
            _playerData[id].LastPosition = newPosition;
            
            // Create the packet in advance
            var positionUpdatePacket = new ClientPlayerPositionUpdatePacket {
                Id = id,
                Position = newPosition
            };
            positionUpdatePacket.CreatePacket();

            // Send the packet to all clients in the same scene
            SendPacketToClientsInSameScene(positionUpdatePacket, false, currentScene, id);
        }
        
        private void OnPlayerUpdateScale(int id, ServerPlayerScaleUpdatePacket packet) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"Received PlayerScaleUpdate packet, but player with ID {id} is not in mapping");
                return;
            }
            
            // Get current scene of player
            var currentScene = _playerData[id].CurrentScene;
            
            var newScale = packet.Scale;
            
            // Store the new position in the player data
            _playerData[id].LastScale = newScale;
            
            // Create the packet in advance
            var scaleUpdatePacket = new ClientPlayerScaleUpdatePacket {
                Id = id,
                Scale = newScale
            };
            scaleUpdatePacket.CreatePacket();

            // Send the packet to all clients in the same scene
            SendPacketToClientsInSameScene(scaleUpdatePacket, false, currentScene, id);
        }
        
        private void OnPlayerUpdateAnimation(int id, ServerPlayerAnimationUpdatePacket packet) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"Received PlayerAnimationUpdate packet, but player with ID {id} is not in mapping");
                return;
            }
            
            // Get current scene of player
            var currentScene = _playerData[id].CurrentScene;
            
            // Get the clip name from the packet
            var clipName = packet.AnimationClipName;
            
            // Get the frame from the packet
            var frame = packet.Frame;
            
            // Get the boolean list of effect info
            var effectInfo = packet.EffectInfo;

            // Store the new animation in the player data
            _playerData[id].LastAnimationClip = clipName;
            
            // Create the packet in advance
            var animationUpdatePacket = new ClientPlayerAnimationUpdatePacket {
                Id = id,
                ClipName = clipName,
                Frame = frame,
                
                EffectInfo = effectInfo
            };
            animationUpdatePacket.CreatePacket();

            // Send the packet to all clients in the same scene
            SendPacketToClientsInSameScene(animationUpdatePacket, false, currentScene, id);
        }

        private void OnPlayerDisconnect(int id, Packet packet) {
            // Always propagate this packet to the NetServer
            _netServer.OnClientDisconnect(id);

            if (!_playerData.ContainsKey(id)) {
                Logger.Warn(this, $"Received Disconnect packet, but player with ID {id} is not in mapping");
                return;
            }

            Logger.Info(this, $"Received Disconnect packet from ID {id}");

            // Get the scene that client was in while disconnecting
            var currentScene = _playerData[id].CurrentScene;

            // Create a PlayerLeaveScene packet containing the ID
            // of the player disconnecting
            var leaveScenePacket = new PlayerLeaveScenePacket {
                Id = id
            };
            leaveScenePacket.CreatePacket();

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
            var playerDeathPacket = new ClientPlayerDeathPacket {
                Id = id
            };
            playerDeathPacket.CreatePacket();
            
            // Send the packet to all clients in the same scene
            SendPacketToClientsInSameScene(playerDeathPacket, true, currentScene, id);
        }

        private void OnServerShutdown() {
            // Clear all existing player data
            _playerData.Clear();
        }

        private void OnApplicationQuit() {
            Stop();
        }

        private void SendPacketToClientsInSameScene(Packet packet, bool tcp, string targetScene, int excludeId) {
            foreach (var idScenePair in _playerData) {
                if (idScenePair.Key == excludeId) {
                    continue;
                }
                
                if (idScenePair.Value.CurrentScene.Equals(targetScene)) {
                    if (tcp) {
                        _netServer.SendTcp(idScenePair.Key, packet);   
                    } else {
                        _netServer.SendUdp(idScenePair.Key, packet);
                    }
                }
            }
        }

    }
}