using System;
using System.Threading;
using HKMP.Concurrency;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Data;

namespace HKMP.Game.Server {
    /**
     * Class that manages the server state (similar to ClientManager).
     * For example the current scene of each player, to prevent sending redundant traffic.
     */
    public class ServerManager {
        private const int ConnectionTimeout = 5000;

        private readonly NetServer _netServer;

        private readonly Game.Settings.GameSettings _gameSettings;

        private readonly ConcurrentDictionary<ushort, ServerPlayerData> _playerData;

        private bool _checkHeartBeat;
        private Thread _heartBeatThread;

        public ServerManager(
            NetServer netServer, 
            Game.Settings.GameSettings gameSettings, 
            PacketManager packetManager
        ) {
            _netServer = netServer;
            _gameSettings = gameSettings;
            _playerData = new ConcurrentDictionary<ushort, ServerPlayerData>();

            // Register packet handlers
            packetManager.RegisterServerPacketHandler<HelloServer>(ServerPacketId.HelloServer, OnHelloServer);
            packetManager.RegisterServerPacketHandler<ServerPlayerEnterScene>(ServerPacketId.PlayerEnterScene, OnClientEnterScene);
            packetManager.RegisterServerPacketHandler(ServerPacketId.PlayerLeaveScene, OnClientLeaveScene);
            packetManager.RegisterServerPacketHandler<PlayerUpdate>(ServerPacketId.PlayerUpdate, OnPlayerUpdate);
            packetManager.RegisterServerPacketHandler<EntityUpdate>(ServerPacketId.EntityUpdate, OnEntityUpdate);
            packetManager.RegisterServerPacketHandler(ServerPacketId.PlayerDisconnect, OnPlayerDisconnect);
            packetManager.RegisterServerPacketHandler(ServerPacketId.PlayerDeath, OnPlayerDeath);
            packetManager.RegisterServerPacketHandler<ServerPlayerTeamUpdate>(ServerPacketId.PlayerTeamUpdate, OnPlayerTeamUpdate);
            packetManager.RegisterServerPacketHandler<ServerPlayerSkinUpdate>(ServerPacketId.PlayerSkinUpdate, OnPlayerSkinUpdate);
            packetManager.RegisterServerPacketHandler<ServerPlayerEmoteUpdate>(ServerPacketId.PlayerEmoteUpdate, OnPlayerEmoteUpdate);

            // Register a heartbeat handler
            _netServer.RegisterOnClientHeartBeat(OnClientHeartBeat);
            
            // Register server shutdown handler
            _netServer.RegisterOnShutdown(OnServerShutdown);
            
            // TODO: make game/console app independent quit handler
            // Register application quit handler
            // ModHooks.Instance.ApplicationQuitHook += OnApplicationQuit;
        }

        /**
         * Starts a server with the given port
         */
        public void Start(int port) {
            // Stop existing server
            if (_netServer.IsStarted) {
                Logger.Get().Warn(this, "Server was running, shutting it down before starting");
                _netServer.Stop();
            }

            // Start server again with given port
            _netServer.Start(port);

            _checkHeartBeat = true;
            
            _heartBeatThread = new Thread(() => {
                while (_checkHeartBeat) {
                    Thread.Sleep(100);

                    CheckHeartBeat();
                }
            });
            _heartBeatThread.Start();
        }

        /**
         * Stops the currently running server
         */
        public void Stop() {
            if (_netServer.IsStarted) {
                // Before shutting down, send TCP packets to all clients indicating
                // that the server is shutting down
                _netServer.SetDataForAllClients(updateManager => {
                    updateManager.SetShutdown();
                });
                
                _netServer.Stop();
            } else {
                Logger.Get().Warn(this, "Could not stop server, it was not started");
            }
        }

        /**
         * Called when the game settings are updated, and need to be broadcast
         */
        public void OnUpdateGameSettings() {
            if (!_netServer.IsStarted) {
                return;
            }
            
            _netServer.SetDataForAllClients(updateManager => {
                updateManager.UpdateGameSettings(_gameSettings);
            });
        }

        /**
         * Get an array of player names
         */
        public string[] GetPlayerNames() {
            var players = _playerData.GetCopy().Values;
            var playerNames = new string[players.Count];
            var i = 0;
            
            foreach (var player in players) {
                playerNames[i++] = player.Username;
            }

            return playerNames;
        }

        private void OnHelloServer(ushort id, HelloServer helloServer) {
            Logger.Get().Info(this, $"Received HelloServer data from ID {id}");
            
            // Start by sending the new client the current Server Settings
            _netServer.GetUpdateManagerForClient(id).UpdateGameSettings(_gameSettings);

            // Create new player data object
            var playerData = new ServerPlayerData(
                helloServer.Username,
                helloServer.SceneName,
                helloServer.Position,
                helloServer.Scale,
                helloServer.AnimationClipId
            );
            // Store data in mapping
            _playerData[id] = playerData;
            
            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key).AddPlayerConnectData(
                    id,
                    helloServer.Username
                );
            }

            OnClientEnterScene(id, playerData);
        }

        private void OnClientEnterScene(ushort id, ServerPlayerEnterScene playerEnterScene) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received EnterScene data from {id}, but player is not in mapping");
                return;
            }
            
            var newSceneName = playerEnterScene.NewSceneName;
            
            Logger.Get().Info(this, $"Received EnterScene data from ID {id}, new scene: {newSceneName}");
            
            // Store it in their PlayerData object
            playerData.CurrentScene = newSceneName;
            playerData.LastPosition = playerEnterScene.Position;
            playerData.LastScale = playerEnterScene.Scale;
            playerData.LastAnimationClip = playerEnterScene.AnimationClipId;

            OnClientEnterScene(id, playerData);
        }

        private void OnClientEnterScene(ushort id, ServerPlayerData playerData) {
            var alreadyPlayersInScene = false;

            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                // Skip source player
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                var otherPlayerData = idPlayerDataPair.Value;

                // Send the packet to all clients on the new scene
                // to indicate that this client has entered their scene
                if (otherPlayerData.CurrentScene.Equals(playerData.CurrentScene)) {
                    Logger.Get().Info(this, $"Sending EnterScene data to {idPlayerDataPair.Key}");

                    _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key).AddPlayerEnterSceneData(
                        id,
                        playerData.Username,
                        playerData.LastPosition,
                        playerData.LastScale,
                        playerData.Team,
                        playerData.SkinId,
                        playerData.LastAnimationClip
                    );

                    Logger.Get().Info(this, $"Sending that {idPlayerDataPair.Key} is already in scene to {id}");

                    alreadyPlayersInScene = true;

                    // Also send a packet to the client that switched scenes,
                    // notifying that these players are already in this new scene.
                    _netServer.GetUpdateManagerForClient(id).AddPlayerAlreadyInSceneData(
                        idPlayerDataPair.Key,
                        otherPlayerData.Username,
                        otherPlayerData.LastPosition,
                        otherPlayerData.LastScale,
                        otherPlayerData.Team,
                        otherPlayerData.SkinId,
                        otherPlayerData.LastAnimationClip
                    );
                }
            }
            
            if (!alreadyPlayersInScene) {
                _netServer.GetUpdateManagerForClient(id).SetAlreadyInSceneHost();
            }
        }

        private void OnClientLeaveScene(ushort id) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received LeaveScene data from {id}, but player is not in mapping");
                return;
            }

            var sceneName = playerData.CurrentScene;

            if (sceneName.Length == 0) {
                Logger.Get().Info(this, $"Received LeaveScene data from ID {id}, but there was no last scene registered");
                return;
            }
            
            Logger.Get().Info(this, $"Received LeaveScene data from ID {id}, last scene: {sceneName}");
            
            playerData.CurrentScene = "";

            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                // Skip source player
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                var otherPlayerData = idPlayerDataPair.Value;

                // Send the packet to all clients on the scene that the player left
                // to indicate that this client has left their scene
                if (otherPlayerData.CurrentScene.Equals(sceneName)) {
                    Logger.Get().Info(this, $"Sending leave scene packet to {idPlayerDataPair.Key}");

                    _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key).AddPlayerLeaveSceneData(id);
                }
            }
        }

        private void OnPlayerUpdate(ushort id, PlayerUpdate playerUpdate) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received PlayerUpdate data, but player with ID {id} is not in mapping");
                return;
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Position)) {
                playerData.LastPosition = playerUpdate.Position;

                SendDataInSameScene(id, otherId => {
                    _netServer.GetUpdateManagerForClient(otherId).UpdatePlayerPosition(id, playerUpdate.Position);
                });
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Scale)) {
                playerData.LastScale = playerUpdate.Scale;

                SendDataInSameScene(id, otherId => {
                    _netServer.GetUpdateManagerForClient(otherId).UpdatePlayerScale(id, playerUpdate.Scale);
                });
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.MapPosition)) {
                playerData.LastMapPosition = playerUpdate.MapPosition;

                // If the map icons need to be broadcast, we add the data to the next packet
                if (_gameSettings.AlwaysShowMapIcons || _gameSettings.OnlyBroadcastMapIconWithWaywardCompass) {
                    foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                        if (idPlayerDataPair.Key == id) {
                            continue;
                        }
                        
                        _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key)
                            .UpdatePlayerMapPosition(id, playerUpdate.MapPosition);
                    }
                }
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Animation)) {
                var animationInfos = playerUpdate.AnimationInfos;

                // Check whether there is any animation info to be stored
                if (animationInfos.Count != 0) {
                    // Set the last animation clip to be the last clip in the animation info list
                    // Since that is the last clip that the player updated
                    playerData.LastAnimationClip = animationInfos[animationInfos.Count - 1].ClipId;

                    // Set the animation data for each player in the same scene
                    SendDataInSameScene(id, otherId => {
                        foreach (var animationInfo in animationInfos) {
                            _netServer.GetUpdateManagerForClient(otherId).UpdatePlayerAnimation(
                                id,
                                animationInfo.ClipId,
                                animationInfo.Frame,
                                animationInfo.EffectInfo
                            );
                        }
                    });
                }
            }
        }

        private void OnEntityUpdate(ushort id, EntityUpdate entityUpdate) {
            if (!_playerData.TryGetValue(id, out _)) {
                Logger.Get().Warn(this, $"Received EntityUpdate data, but player with ID {id} is not in mapping");
                return;
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Position)) {
                SendDataInSameScene(id, otherId => {
                    _netServer.GetUpdateManagerForClient(otherId).UpdateEntityPosition(
                        entityUpdate.EntityType,
                        entityUpdate.Id,
                        entityUpdate.Position
                    );
                });
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.State)) {
                SendDataInSameScene(id, otherId => {
                    _netServer.GetUpdateManagerForClient(otherId).UpdateEntityState(
                        entityUpdate.EntityType,
                        entityUpdate.Id,
                        entityUpdate.State
                    );
                });
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Variables)) {
                SendDataInSameScene(id, otherId => {
                    _netServer.GetUpdateManagerForClient(otherId).UpdateEntityVariables(
                        entityUpdate.EntityType,
                        entityUpdate.Id,
                        entityUpdate.Variables
                    );
                });
            }
        }

        private void OnPlayerDisconnect(ushort id) {
            // Always propagate this packet to the NetServer
            _netServer.OnClientDisconnect(id);

            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received PlayerDisconnect data, but player with ID {id} is not in mapping");
                return;
            }

            Logger.Get().Info(this, $"Received PlayerDisconnect data from ID: {id}");

            var username = playerData.Username;

            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key).AddPlayerDisconnectData(
                    id,
                    username
                );
            }

            // Now remove the client from the player data mapping
            _playerData.Remove(id);
        }

        private void OnPlayerDeath(ushort id) {
            if (!_playerData.TryGetValue(id, out _)) {
                Logger.Get().Warn(this, $"Received PlayerDeath data, but player with ID {id} is not in mapping");
                return;
            }

            Logger.Get().Info(this, $"Received PlayerDeath data from ID {id}");
            
            SendDataInSameScene(id, otherId => {
                _netServer.GetUpdateManagerForClient(otherId).AddPlayerDeathData(id);
            });
        }

        private void OnPlayerTeamUpdate(ushort id, ServerPlayerTeamUpdate teamUpdate) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received PlayerTeamUpdate data, but player with ID {id} is not in mapping");
                return;
            }

            Logger.Get().Info(this, $"Received PlayerTeamUpdate data from ID: {id}, new team: {teamUpdate.Team}");

            // Update the team in the player data
            playerData.Team = teamUpdate.Team;

            // Broadcast the packet to all players except the player we received the update from
            foreach (var playerId in _playerData.GetCopy().Keys) {
                if (id == playerId) {
                    continue;
                }
                
                _netServer.GetUpdateManagerForClient(playerId).AddPlayerTeamUpdateData(
                    id,
                    playerData.Username,
                    teamUpdate.Team
                );
            }
        }
        
        private void OnPlayerSkinUpdate(ushort id, ServerPlayerSkinUpdate skinUpdate) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received PlayerSkinUpdate data, but player with ID {id} is not in mapping");
                return;
            }

            if (playerData.SkinId == skinUpdate.SkinId) {
                Logger.Get().Info(this, $"Received PlayerSkinUpdate data from ID: {id}, but skin was the same");
                return;
            }

            Logger.Get().Info(this, $"Received PlayerSkinUpdate data from ID: {id}, new skin ID: {skinUpdate.SkinId}");
            
            // Update the skin ID in the player data
            playerData.SkinId = skinUpdate.SkinId;
            
            SendDataInSameScene(id, otherId => {
                _netServer.GetUpdateManagerForClient(otherId).AddPlayerSkinUpdateData(id, playerData.SkinId);
            });
        }
        
        private void OnPlayerEmoteUpdate(ushort id, ServerPlayerEmoteUpdate emoteUpdate) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received PlayerEmoteUpdate data, but player with ID {id} is not in mapping");
                return;
            }

            Logger.Get().Info(this, $"Received PlayerEmoteUpdate data from ID: {id}, emote ID: {emoteUpdate.EmoteId}");

            SendDataInSameScene(id, otherId => {
                _netServer.GetUpdateManagerForClient(otherId).AddPlayerEmoteUpdateData(id, emoteUpdate.EmoteId);
            });
        }

        private void OnServerShutdown() {
            // Clear all existing player data
            _playerData.Clear();

            _checkHeartBeat = false;
            _heartBeatThread.Abort();
        }

        private void OnApplicationQuit() {
            Stop();
        }

        private void OnClientHeartBeat(ushort id) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received heart beat from unknown player with ID: {id}");
                return;
            }

            // Since we received a heart beat from the player, we can reset their heart beat stopwatch
            playerData.HeartBeatStopwatch.Reset();
            playerData.HeartBeatStopwatch.Start();
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
                    Logger.Get().Info(this,
                        $"Didn't receive heart beat from player {id} in {ConnectionTimeout} milliseconds, dropping client");
                    OnPlayerDisconnect(id);
                }
            }
        }

        private void SendDataInSameScene(ushort sourceId, Action<ushort> dataAction) {
            var playerData = _playerData.GetCopy();

            foreach (var idPlayerDataPair in playerData) {
                // Skip sending to same ID
                if (idPlayerDataPair.Key == sourceId) {
                    continue;
                }

                var otherPd = idPlayerDataPair.Value;

                // Skip sending to players not in the same scene
                if (!otherPd.CurrentScene.Equals(playerData[sourceId].CurrentScene)) {
                    continue;
                }

                dataAction(idPlayerDataPair.Key);
            }
        }
    }
}