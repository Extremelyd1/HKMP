using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Api.Server;
using Hkmp.Concurrency;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Networking.Server;

namespace Hkmp.Game.Server {
    /**
     * Class that manages the server state (similar to ClientManager).
     * For example the current scene of each player, to prevent sending redundant traffic.
     */
    public abstract class ServerManager : IServerManager {
        #region Internal server manager variables and properties

        private readonly NetServer _netServer;

        private readonly Settings.GameSettings _gameSettings;

        private readonly ConcurrentDictionary<ushort, ServerPlayerData> _playerData;

        private readonly ServerAddonManager _addonManager;

        #endregion

        #region IServerManager properties

        public event Action<IServerPlayer> ConnectEvent;
        public event Action<IServerPlayer> DisconnectEvent;
        public IReadOnlyCollection<IServerPlayer> Players => _playerData.GetCopy().Values;
        
        #endregion

        protected ServerManager(
            NetServer netServer,
            Settings.GameSettings gameSettings,
            PacketManager packetManager
        ) {
            _netServer = netServer;
            _gameSettings = gameSettings;
            _playerData = new ConcurrentDictionary<ushort, ServerPlayerData>();

            var serverApi = new ServerApi(this, _netServer);
            _addonManager = new ServerAddonManager(serverApi);

            // Register packet handlers
            packetManager.RegisterServerPacketHandler<HelloServer>(ServerPacketId.HelloServer, OnHelloServer);
            packetManager.RegisterServerPacketHandler<ServerPlayerEnterScene>(ServerPacketId.PlayerEnterScene,
                OnClientEnterScene);
            packetManager.RegisterServerPacketHandler(ServerPacketId.PlayerLeaveScene, OnClientLeaveScene);
            packetManager.RegisterServerPacketHandler<PlayerUpdate>(ServerPacketId.PlayerUpdate, OnPlayerUpdate);
            packetManager.RegisterServerPacketHandler<EntityUpdate>(ServerPacketId.EntityUpdate, OnEntityUpdate);
            packetManager.RegisterServerPacketHandler(ServerPacketId.PlayerDisconnect, OnPlayerDisconnect);
            packetManager.RegisterServerPacketHandler(ServerPacketId.PlayerDeath, OnPlayerDeath);
            packetManager.RegisterServerPacketHandler<ServerPlayerTeamUpdate>(ServerPacketId.PlayerTeamUpdate,
                OnPlayerTeamUpdate);
            packetManager.RegisterServerPacketHandler<ServerPlayerSkinUpdate>(ServerPacketId.PlayerSkinUpdate,
                OnPlayerSkinUpdate);

            // Register a timeout handler
            _netServer.ClientTimeoutEvent += OnClientTimeout;

            // Register server shutdown handler
            _netServer.ShutdownEvent += OnServerShutdown;

            // Register a handler for when a client wants to login
            _netServer.LoginRequestEvent += OnLoginRequest;
        }

        #region Internal server manager methods

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
        }

        /**
         * Stops the currently running server
         */
        public void Stop() {
            if (_netServer.IsStarted) {
                // Before shutting down, send TCP packets to all clients indicating
                // that the server is shutting down
                _netServer.SetDataForAllClients(updateManager => { updateManager.SetShutdown(); });

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

            _netServer.SetDataForAllClients(updateManager => { updateManager.UpdateGameSettings(_gameSettings); });
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
            _netServer.GetUpdateManagerForClient(id)?.UpdateGameSettings(_gameSettings);

            // Create new player data object
            var playerData = new ServerPlayerData(
                id,
                helloServer.Username,
                helloServer.SceneName,
                helloServer.Position,
                helloServer.Scale,
                helloServer.AnimationClipId
            );
            // Store data in mapping
            _playerData[id] = playerData;

            try {
                ConnectEvent?.Invoke(playerData);
            } catch (Exception e) {
                Logger.Get().Warn(this, $"Exception thrown while invoking connect event, {e.GetType()}, {e.Message}, {e.StackTrace}");
            }

            var clientInfo = new List<(ushort, string)>();

            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                if (idPlayerDataPair.Key == id) {
                    continue;
                }
                
                clientInfo.Add((idPlayerDataPair.Key, idPlayerDataPair.Value.Username));
                
                _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key)?.AddPlayerConnectData(
                    id,
                    helloServer.Username
                );
            }

            _netServer.GetUpdateManagerForClient(id).SetHelloClientData(clientInfo);

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
            playerData.Position = playerEnterScene.Position;
            playerData.Scale = playerEnterScene.Scale;
            playerData.AnimationId = playerEnterScene.AnimationClipId;

            OnClientEnterScene(id, playerData);
        }

        private void OnClientEnterScene(ushort id, ServerPlayerData playerData) {
            var enterSceneList = new List<ClientPlayerEnterScene>();
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

                    _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key)?.AddPlayerEnterSceneData(
                        id,
                        playerData.Username,
                        playerData.Position,
                        playerData.Scale,
                        playerData.Team,
                        playerData.SkinId,
                        playerData.AnimationId
                    );

                    Logger.Get().Info(this, $"Sending that {idPlayerDataPair.Key} is already in scene to {id}");

                    alreadyPlayersInScene = true;

                    // Also send a packet to the client that switched scenes,
                    // notifying that these players are already in this new scene.
                    enterSceneList.Add(new ClientPlayerEnterScene {
                        Id = idPlayerDataPair.Key,
                        Username = otherPlayerData.Username,
                        Position = otherPlayerData.Position,
                        Scale = otherPlayerData.Scale,
                        Team = otherPlayerData.Team,
                        SkinId = otherPlayerData.SkinId,
                        AnimationClipId = otherPlayerData.AnimationId
                    });
                }
            }

            _netServer.GetUpdateManagerForClient(id)?.AddPlayerAlreadyInSceneData(
                enterSceneList,
                !alreadyPlayersInScene
            );
        }

        private void OnClientLeaveScene(ushort id) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received LeaveScene data from {id}, but player is not in mapping");
                return;
            }

            var sceneName = playerData.CurrentScene;

            if (sceneName.Length == 0) {
                Logger.Get().Info(this,
                    $"Received LeaveScene data from ID {id}, but there was no last scene registered");
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

                    _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key)?.AddPlayerLeaveSceneData(id);
                }
            }
        }

        private void OnPlayerUpdate(ushort id, PlayerUpdate playerUpdate) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received PlayerUpdate data, but player with ID {id} is not in mapping");
                return;
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Position)) {
                playerData.Position = playerUpdate.Position;

                SendDataInSameScene(id,
                    otherId => {
                        _netServer.GetUpdateManagerForClient(otherId)?.UpdatePlayerPosition(id, playerUpdate.Position);
                    });
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Scale)) {
                playerData.Scale = playerUpdate.Scale;

                SendDataInSameScene(id,
                    otherId => {
                        _netServer.GetUpdateManagerForClient(otherId)?.UpdatePlayerScale(id, playerUpdate.Scale);
                    });
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.MapPosition)) {
                playerData.MapPosition = playerUpdate.MapPosition;

                // If the map icons need to be broadcast, we add the data to the next packet
                if (_gameSettings.AlwaysShowMapIcons || _gameSettings.OnlyBroadcastMapIconWithWaywardCompass) {
                    foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                        if (idPlayerDataPair.Key == id) {
                            continue;
                        }

                        _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key)?
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
                    playerData.AnimationId = animationInfos[animationInfos.Count - 1].ClipId;

                    // Set the animation data for each player in the same scene
                    SendDataInSameScene(id, otherId => {
                        foreach (var animationInfo in animationInfos) {
                            _netServer.GetUpdateManagerForClient(otherId)?.UpdatePlayerAnimation(
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
                    _netServer.GetUpdateManagerForClient(otherId)?.UpdateEntityPosition(
                        entityUpdate.EntityType,
                        entityUpdate.Id,
                        entityUpdate.Position
                    );
                });
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.State)) {
                SendDataInSameScene(id, otherId => {
                    _netServer.GetUpdateManagerForClient(otherId)?.UpdateEntityState(
                        entityUpdate.EntityType,
                        entityUpdate.Id,
                        entityUpdate.State
                    );
                });
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Variables)) {
                SendDataInSameScene(id, otherId => {
                    _netServer.GetUpdateManagerForClient(otherId)?.UpdateEntityVariables(
                        entityUpdate.EntityType,
                        entityUpdate.Id,
                        entityUpdate.Variables
                    );
                });
            }
        }

        /**
         * Callback for when a packet with disconnect data is received
         */
        private void OnPlayerDisconnect(ushort id) {
            if (!_playerData.TryGetValue(id, out _)) {
                Logger.Get().Warn(this, $"Received PlayerDisconnect data, but player with ID {id} is not in mapping");
                return;
            }

            Logger.Get().Info(this, $"Received PlayerDisconnect data from ID: {id}");

            DisconnectPlayer(id);
        }

        private void DisconnectPlayer(ushort id, bool timeout = false) {
            if (!timeout) {
                // If this isn't a timeout, then we need to propagate this packet to the NetServer
                _netServer.OnClientDisconnect(id);
            }

            if (!_playerData.TryGetValue(id, out var playerData)) {
                return;
            }
            
            try {
                DisconnectEvent?.Invoke(playerData);
            } catch (Exception e) {
                Logger.Get().Warn(this, $"Exception thrown while invoking disconnect event, {e.GetType()}, {e.Message}, {e.StackTrace}");
            }

            var username = playerData.Username;

            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                if (idPlayerDataPair.Key == id) {
                    continue;
                }

                _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key)?.AddPlayerDisconnectData(
                    id,
                    username,
                    timeout
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

            SendDataInSameScene(id,
                otherId => { _netServer.GetUpdateManagerForClient(otherId)?.AddPlayerDeathData(id); });
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

                _netServer.GetUpdateManagerForClient(playerId)?.AddPlayerTeamUpdateData(
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

            SendDataInSameScene(id,
                otherId => {
                    _netServer.GetUpdateManagerForClient(otherId)?.AddPlayerSkinUpdateData(id, playerData.SkinId);
                });
        }

        private void OnServerShutdown() {
            // Clear all existing player data
            _playerData.Clear();
        }

        private void HandleInvalidLoginAddons(ServerUpdateManager updateManager) {
            var loginResponse = new LoginResponse {
                LoginResponseStatus = LoginResponseStatus.InvalidAddons
            };
            loginResponse.AddonData.AddRange(_addonManager.GetNetworkedAddonData());

            updateManager.SetLoginResponse(loginResponse);
        }

        private bool OnLoginRequest(LoginRequest loginRequest, ServerUpdateManager updateManager) {
            Logger.Get().Info(this, $"Received login request from username: {loginRequest.Username}");

            // Check whether the username is not already in use
            foreach (var playerData in _playerData.GetCopy().Values) {
                if (playerData.Username.ToLower().Equals(loginRequest.Username.ToLower())) {
                    updateManager.SetLoginResponse(new LoginResponse {
                        LoginResponseStatus = LoginResponseStatus.InvalidUsername
                    });
                    return false;
                }
            }
            
            var addonData = loginRequest.AddonData;

            // Construct a string that contains all addons and respective versions by mapping the items in the addon data
            var addonStringList = string.Join(", ", addonData.Select(addon => $"{addon.Identifier} v{addon.Version}"));
            Logger.Get().Info(this, $"  Client tries to connect with following addons: {addonStringList}");

            // If there is a mismatch between the number of networked addons of the client and the server,
            // we can immediately invalidate the request
            if (addonData.Count != _addonManager.GetNetworkedAddonData().Count) {
                HandleInvalidLoginAddons(updateManager);
                return false;
            }

            // Create a byte list denoting the order of the addons on the server
            var addonOrder = new List<byte>();

            foreach (var addon in addonData) {
                // Check and retrieve the server addon with the same name and version
                if (!_addonManager.TryGetNetworkedAddon(
                        addon.Identifier,
                        addon.Version,
                        out var correspondingServerAddon
                    )) {
                    // There was no corresponding server addon, so we send a login response with an invalid status
                    // and the addon data that is present on the server, so the client knows what is invalid
                    HandleInvalidLoginAddons(updateManager);
                    return false;
                }

                // If the addon is also present on the server, we append the addon order with the correct index
                addonOrder.Add(correspondingServerAddon.Id);
            }

            var loginResponse = new LoginResponse {
                LoginResponseStatus = LoginResponseStatus.Success,
                AddonOrder = addonOrder.ToArray()
            };

            updateManager.SetLoginResponse(loginResponse);

            return true;
        }

        /**
         * Callback for when a client times out
         */
        private void OnClientTimeout(ushort id) {
            if (!_playerData.TryGetValue(id, out _)) {
                Logger.Get().Warn(this, $"Received timeout from unknown player with ID: {id}");
                return;
            }

            // Since the client has timed out, we can formally disconnect them
            DisconnectPlayer(id, true);
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

        #endregion

        #region IServerManager methods

        public IServerPlayer GetPlayer(ushort id) {
            return TryGetPlayer(id, out var player) ? player : null;
        }

        public bool TryGetPlayer(ushort id, out IServerPlayer player) {
            var found = _playerData.TryGetValue(id, out var playerData);
            player = playerData;

            return found;
        }

        #endregion
    }
}