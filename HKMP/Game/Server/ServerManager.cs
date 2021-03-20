using HKMP.Concurrency;
using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
using HKMP.Networking.Server;
using HKMP.Util;
using Modding;

namespace HKMP.Game.Server {
    /**
     * Class that manages the server state (similar to ClientManager).
     * For example the current scene of each player, to prevent sending redundant traffic.
     */
    public class ServerManager {
        private const int ConnectionTimeout = 5000;

        private readonly NetServer _netServer;

        private readonly Game.Settings.GameSettings _gameSettings;

        private readonly ConcurrentDictionary<ushort, PlayerData> _playerData;
        
        public ServerManager(NetworkManager networkManager, Game.Settings.GameSettings gameSettings, PacketManager packetManager) {
            _netServer = networkManager.GetNetServer();
            _gameSettings = gameSettings;

            _playerData = new ConcurrentDictionary<ushort, PlayerData>();

            // Register packet handlers
            packetManager.RegisterServerPacketHandler<HelloServerPacket>(PacketId.HelloServer, OnHelloServer);
            packetManager.RegisterServerPacketHandler<ServerPlayerEnterScenePacket>(PacketId.PlayerEnterScene, OnClientEnterScene);
            packetManager.RegisterServerPacketHandler<ServerPlayerLeaveScenePacket>(PacketId.PlayerLeaveScene, OnClientLeaveScene);
            packetManager.RegisterServerPacketHandler<ServerPlayerUpdatePacket>(PacketId.PlayerUpdate, OnPlayerUpdate);
            packetManager.RegisterServerPacketHandler<ServerPlayerDisconnectPacket>(PacketId.PlayerDisconnect, OnPlayerDisconnect);
            packetManager.RegisterServerPacketHandler<ServerPlayerDeathPacket>(PacketId.PlayerDeath, OnPlayerDeath);
            packetManager.RegisterServerPacketHandler<ServerDreamshieldSpawnPacket>(PacketId.DreamshieldSpawn, OnDreamshieldSpawn);
            packetManager.RegisterServerPacketHandler<ServerDreamshieldDespawnPacket>(PacketId.DreamshieldDespawn, OnDreamshieldDespawn);
            packetManager.RegisterServerPacketHandler<ServerDreamshieldUpdatePacket>(PacketId.DreamshieldUpdate, OnDreamshieldUpdate);
            
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

            MonoBehaviourUtil.Instance.OnUpdateEvent += CheckHeartBeat;
        }

        /**
         * Stops the currently running server
         */
        public void Stop() {
            if (_netServer.IsStarted) {
                // Before shutting down, send TCP packets to all clients indicating
                // that the server is shutting down
                _netServer.BroadcastTcp(new ServerShutdownPacket().CreatePacket());
                
                _netServer.Stop();
            } else {
                Logger.Warn(this, "Could not stop server, it was not started");
            }
        }

        /**
         * Called when the game settings are updated, and need to be broadcast
         */
        public void OnUpdateGameSettings() {
            if (!_netServer.IsStarted) {
                return;
            }
        
            var settingsUpdatePacket = new GameSettingsUpdatePacket {
                GameSettings = _gameSettings
            };
            settingsUpdatePacket.CreatePacket();
            
            _netServer.BroadcastTcp(settingsUpdatePacket);
        }

        private void OnHelloServer(ushort id, HelloServerPacket packet) {
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
            var currentClip = packet.AnimationClipId;
            
            // Create new player data object
            var playerData = new PlayerData(
                username,
                sceneName,
                position,
                scale,
                currentClip
            );
            // Store data in mapping
            _playerData[id] = playerData;

            // Create PlayerConnect packet
            var playerConnectPacket = new ClientPlayerConnectPacket {
                Id = id,
                Username = username
            };
            playerConnectPacket.CreatePacket();
            
            // Create PlayerEnterScene packet
            var enterScenePacket = new ClientPlayerEnterScenePacket {
                Id = id,
                Username = username,
                Scale = scale,
                AnimationClipId = currentClip
            };
            enterScenePacket.CreatePacket();
            
            // Loop over all other clients and skip over the client that just connected
            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                var otherPlayerData = idPlayerDataPair.Value;
                
                // Send the PlayerConnect packet to all other clients
                _netServer.SendTcp(idPlayerDataPair.Key, playerConnectPacket);
                
                // Send the EnterScene packet only to clients in the same scene
                if (otherPlayerData.CurrentScene.Equals(sceneName)) {
                    _netServer.SendTcp(idPlayerDataPair.Key, enterScenePacket);
                    
                    // Also send the source client a packet that this player is in their scene
                    var alreadyInScenePacket = new ClientPlayerEnterScenePacket {
                        Id = idPlayerDataPair.Key,
                        Username = otherPlayerData.Name,
                        Position = otherPlayerData.LastPosition,
                        Scale = otherPlayerData.LastScale,
                        AnimationClipId = otherPlayerData.LastAnimationClip
                    };
                    alreadyInScenePacket.CreatePacket();
                    
                    _netServer.SendTcp(id, alreadyInScenePacket);
                }
            }
        }

        private void OnClientEnterScene(ushort id, ServerPlayerEnterScenePacket packet) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Warn(this, $"Received EnterScene packet from {id}, but player is not in mapping");
                return;
            }
            
            // Read the values in from the packet
            var newSceneName = packet.NewSceneName;
            var position = packet.Position;
            var scale = packet.Scale;
            var animationClipId = packet.AnimationClipId;
            
            Logger.Info(this, $"Received EnterScene packet from ID {id}, new scene: {newSceneName}");
            
            // Store it in their PlayerData object
            playerData.CurrentScene = newSceneName;
            playerData.LastPosition = position;
            playerData.LastScale = scale;
            playerData.LastAnimationClip = animationClipId;
            
            // Create a PlayerEnterScene packet containing the ID
            // of the player entering the scene and the respective values
            var enterScenePacket = new ClientPlayerEnterScenePacket {
                Id = id,
                Username = playerData.Name,
                Position = position,
                Scale = scale,
                AnimationClipId = animationClipId
            };
            enterScenePacket.CreatePacket();

            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                // Skip source player
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                var otherPlayerData = idPlayerDataPair.Value;

                // Send the packet to all clients on the new scene
                // to indicate that this client has entered their scene
                if (otherPlayerData.CurrentScene.Equals(newSceneName)) {
                    Logger.Info(this, $"Sending enter scene packet to {idPlayerDataPair.Key}");
                    _netServer.SendTcp(idPlayerDataPair.Key, enterScenePacket);

                    Logger.Info(this, $"Sending that {idPlayerDataPair.Key} is already in scene to {id}");

                    // Also send a packet to the client that switched scenes,
                    // notifying that these players are already in this new scene
                    var alreadyInScenePacket = new ClientPlayerEnterScenePacket {
                        Id = idPlayerDataPair.Key,
                        Username = otherPlayerData.Name,
                        Position = otherPlayerData.LastPosition,
                        Scale = otherPlayerData.LastScale,
                        AnimationClipId = otherPlayerData.LastAnimationClip
                    };
                    alreadyInScenePacket.CreatePacket();

                    _netServer.SendTcp(id, alreadyInScenePacket);
                }
            }
        }

        private void OnClientLeaveScene(ushort id, ServerPlayerLeaveScenePacket packet) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Warn(this, $"Received LeaveScene packet from {id}, but player is not in mapping");
                return;
            }

            var sceneName = playerData.CurrentScene;

            if (sceneName.Length == 0) {
                Logger.Info(this, $"Received LeaveScene packet from ID {id}, but there was no last scene registered");
                return;
            }
            
            Logger.Info(this, $"Received LeaveScene packet from ID {id}, last scene: {sceneName}");
            
            playerData.CurrentScene = "";
            
            // Create a PlayerLeaveScene packet containing the ID
            // of the player leaving the scene
            var leaveScenePacket = new ClientPlayerLeaveScenePacket {
                Id = id
            };
            leaveScenePacket.CreatePacket();

            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                // Skip source player
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                var otherPlayerData = idPlayerDataPair.Value;

                // Send the packet to all clients on the scene that the player left
                // to indicate that this client has left their scene
                if (otherPlayerData.CurrentScene.Equals(sceneName)) {
                    Logger.Info(this, $"Sending leave scene packet to {idPlayerDataPair.Key}");
                    _netServer.SendTcp(idPlayerDataPair.Key, leaveScenePacket);
                }
            }
        }
        
        // TODO: still need to test whether there are no asynchronous multiple accesses to shared variables
        private void OnPlayerUpdate(ushort id, ServerPlayerUpdatePacket packet) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Warn(this, $"Received PlayerUpdate packet, but player with ID {id} is not in mapping");
                return;
            }
            
            // Since we received an update from the player, we can reset their heart beat stopwatch
            playerData.HeartBeatStopwatch.Reset();
            playerData.HeartBeatStopwatch.Start();

            var playerUpdate = packet.PlayerUpdate;

            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Position)) {
                playerData.LastPosition = playerUpdate.Position;
            }
            
            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Scale)) {
                playerData.LastScale = playerUpdate.Scale;
            }
            
            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.MapPosition)) {
                playerData.LastMapPosition = playerUpdate.MapPosition;
            }

            if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Animation)) {
                var animationInfos = playerUpdate.AnimationInfos;

                // Check whether there is any animation info to be stored
                if (animationInfos.Count != 0) {
                    // Set the last animation clip to be the last clip in the animation info list
                    // Since that is the last clip that the player updated
                    playerData.LastAnimationClip = animationInfos[animationInfos.Count - 1].ClipId;

                    // Now we need to update each playerData instance to include all animation info instances,
                    // that way when we send them an update packet (as response), we can include that animation info
                    // of this player
                    foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                        // Skip over the player that we received from
                        if (idPlayerDataPair.Key == id) {
                            continue;
                        }

                        var otherPd = idPlayerDataPair.Value;

                        // We only queue the animation info if the players are on the same scene,
                        // otherwise the animations get spammed once the players enter the same scene
                        if (!otherPd.CurrentScene.Equals(playerData.CurrentScene)) {
                            continue;
                        }

                        // If the queue did not exist yet, we create it and add it
                        if (!otherPd.AnimationInfoToSend.TryGetValue(id, out var animationInfoQueue)) {
                            animationInfoQueue = new ConcurrentQueue<AnimationInfo>();

                            otherPd.AnimationInfoToSend[id] = animationInfoQueue;
                        } else {
                            animationInfoQueue = otherPd.AnimationInfoToSend[id];
                        }

                        // For each of the animationInfo that the player sent, add them to this other player data instance
                        foreach (var animationInfo in animationInfos) {
                            animationInfoQueue.Enqueue(animationInfo);
                        }
                    }
                }
            }
            
            // Now we need to update the player from which we received an update of all current (and relevant)
            // information of the other players
            var clientPlayerUpdatePacket = new ClientPlayerUpdatePacket();

            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                var otherPd = idPlayerDataPair.Value;

                // Keep track of whether we actually update any value of the player
                // we are looping over, otherwise, we don't have to add the PlayerUpdate instance
                var wasUpdated = false;
                
                // Create a new PlayerUpdate instance
                playerUpdate = new PlayerUpdate {
                    Id = idPlayerDataPair.Key
                };

                // If the players are on the same scene, we need to update
                // position, scale and all unsent animations
                if (playerData.CurrentScene.Equals(otherPd.CurrentScene)) {
                    wasUpdated = true;
                    
                    playerUpdate.UpdateTypes.Add(UpdatePacketType.Position);
                    playerUpdate.Position = otherPd.LastPosition;
                    
                    playerUpdate.UpdateTypes.Add(UpdatePacketType.Scale);
                    playerUpdate.Scale = otherPd.LastScale;

                    // Get the queue of animation info corresponding to the player that we are
                    // currently looping over, which is meant for the player we need to update
                    // If the queue exists and is non-empty, we add the info
                    if (playerData.AnimationInfoToSend.TryGetValue(idPlayerDataPair.Key, out var animationInfoQueue)) {
                        var infoQueueCopy = animationInfoQueue.GetCopy();
                        
                        if (infoQueueCopy.Count != 0) {
                            playerUpdate.UpdateTypes.Add(UpdatePacketType.Animation);
                            playerUpdate.AnimationInfos.AddRange(infoQueueCopy);

                            animationInfoQueue.Clear();
                        }
                    }
                }
                
                // If the map icons need to be broadcast, we add those to the player update
                if (_gameSettings.AlwaysShowMapIcons || _gameSettings.OnlyBroadcastMapIconWithWaywardCompass) {
                    wasUpdated = true;
                    
                    playerUpdate.UpdateTypes.Add(UpdatePacketType.MapPosition);
                    playerUpdate.MapPosition = otherPd.LastMapPosition;
                }

                // Finally, add the finalized playerUpdate instance to the packet
                // However, we only do this if any values were updated
                if (wasUpdated) {
                    clientPlayerUpdatePacket.PlayerUpdates.Add(playerUpdate);
                }
            }
            
            // Once this is done for each player that needs updates,
            // we can send the packet
            _netServer.SendPlayerUpdate(id, clientPlayerUpdatePacket);
        }

        private void OnPlayerDisconnect(ushort id, Packet packet) {
            Logger.Info(this, $"Received Disconnect packet from ID {id}");
            OnPlayerDisconnect(id);
        }

        private void OnPlayerDisconnect(ushort id) {
            // Always propagate this packet to the NetServer
            _netServer.OnClientDisconnect(id);

            if (!_playerData.TryGetValue(id, out _)) {
                Logger.Warn(this, $"Player disconnect, but player with ID {id} is not in mapping");
                return;
            }
            
            // Send a player disconnect packet
            var playerDisconnectPacket = new ClientPlayerDisconnectPacket {
                Id = id,
                Username = _playerData[id].Name
            };
            
            foreach (var idScenePair in _playerData.GetCopy()) {
                if (idScenePair.Key == id) {
                    continue;
                }

                _netServer.SendTcp(idScenePair.Key, playerDisconnectPacket.CreatePacket());
            }

            // Now remove the client from the player data mapping
            _playerData.Remove(id);
        }

        private void OnPlayerDeath(ushort id, Packet packet) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Warn(this, $"Received PlayerDeath packet, but player with ID {id} is not in mapping");
                return;
            }

            Logger.Info(this, $"Received PlayerDeath packet from ID {id}");
            
            // Get the scene that the client was last in
            var currentScene = playerData.CurrentScene;
            
            // Create a new PlayerDeath packet containing the ID of the player that died
            var playerDeathPacket = new ClientPlayerDeathPacket {
                Id = id
            };
            playerDeathPacket.CreatePacket();
            
            // Send the packet to all clients in the same scene
            SendPacketToClientsInSameScene(playerDeathPacket, currentScene, id);
        }
        
        private void OnDreamshieldSpawn(ushort id, ServerDreamshieldSpawnPacket packet) {
            // if (!_playerData.ContainsKey(id)) {
            //     Logger.Warn(this, $"Received DreamshieldSpawn packet, but player with ID {id} is not in mapping");
            //     return;
            // }
            //
            // Logger.Info(this, $"Received DreamshieldSpawn packet from ID {id}");
            //
            // // Get the scene that the client was last in
            // var currentScene = _playerData[id].CurrentScene;
            //
            // // Create a new DreamshieldSpawn packet containing the ID of the player
            // var dreamshieldSpawnPacket = new ClientDreamshieldSpawnPacket {
            //     Id = id
            // };
            // dreamshieldSpawnPacket.CreatePacket();
            //
            // // Send the packet to all clients in the same scene
            // SendPacketToClientsInSameScene(dreamshieldSpawnPacket, false, currentScene, id);
        }
        
        private void OnDreamshieldDespawn(ushort id, ServerDreamshieldDespawnPacket packet) {
            // if (!_playerData.ContainsKey(id)) {
            //     Logger.Warn(this, $"Received DreamshieldDespawn packet, but player with ID {id} is not in mapping");
            //     return;
            // }
            //
            // Logger.Info(this, $"Received DreamshieldDespawn packet from ID {id}");
            //
            // // Get the scene that the client was last in
            // var currentScene = _playerData[id].CurrentScene;
            //
            // // Create a new DreamshieldDespawn packet containing the ID of the player
            // var dreamshieldDespawnPacket = new ClientDreamshieldDespawnPacket {
            //     Id = id
            // };
            // dreamshieldDespawnPacket.CreatePacket();
            //
            // // Send the packet to all clients in the same scene
            // SendPacketToClientsInSameScene(dreamshieldDespawnPacket, false, currentScene, id);
        }

        private void OnDreamshieldUpdate(ushort id, ServerDreamshieldUpdatePacket packet) {
            // if (!_playerData.ContainsKey(id)) {
            //     Logger.Warn(this, $"Received DreamshieldUpdate packet, but player with ID {id} is not in mapping");
            //     return;
            // }
            //
            // // Get the scene that the client was last in
            // var currentScene = _playerData[id].CurrentScene;
            //
            // // Create a new DreamshieldDespawn packet containing the ID of the player
            // var dreamshieldUpdatePacket = new ClientDreamshieldUpdatePacket {
            //     Id = id,
            //     BlockEffect = packet.BlockEffect,
            //     BreakEffect = packet.BreakEffect,
            //     ReformEffect = packet.ReformEffect
            // };
            // dreamshieldUpdatePacket.CreatePacket();
            //
            // // Send the packet to all clients in the same scene
            // SendPacketToClientsInSameScene(dreamshieldUpdatePacket, false, currentScene, id);
        }

        private void OnServerShutdown() {
            // Clear all existing player data
            _playerData.Clear();
            
            // De-register the heart beat update
            MonoBehaviourUtil.Instance.OnUpdateEvent -= CheckHeartBeat;
        }

        private void OnApplicationQuit() {
            Stop();
        }
        
        private void CheckHeartBeat() {
            // The server is not started, so there is no need to check heart beats
            if (!_netServer.IsStarted) {
                return;
            }

            // For each connected client, check whether a heart beat has been received recently
            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                if (idPlayerDataPair.Value.HeartBeatStopwatch.ElapsedMilliseconds > ConnectionTimeout) {
                    // The stopwatch has surpassed the connection timeout value, so we disconnect the client
                    var id = idPlayerDataPair.Key;
                    Logger.Info(this,
                        $"Didn't receive heart beat from player {id} in {ConnectionTimeout} milliseconds, dropping client");
                    OnPlayerDisconnect(id);
                }
            }
        }

        private void SendPacketToClientsInSameScene(Packet packet, string targetScene, int excludeId) {
            foreach (var idScenePair in _playerData.GetCopy()) {
                if (idScenePair.Key == excludeId) {
                    continue;
                }
                
                if (idScenePair.Value.CurrentScene.Equals(targetScene)) {
                    _netServer.SendTcp(idScenePair.Key, packet);
                }
            }
        }

    }
}