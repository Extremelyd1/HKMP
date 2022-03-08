using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Api.Command.Server;
using Hkmp.Api.Server;
using Hkmp.Concurrency;
using Hkmp.Game.Command.Server;
using Hkmp.Game.Server.Auth;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Networking.Server;
using Hkmp.Util;

namespace Hkmp.Game.Server {
    /**
     * Class that manages the server state (similar to ClientManager).
     * For example the current scene of each player, to prevent sending redundant traffic.
     */
    public abstract class ServerManager : IServerManager {
        #region Internal server manager variables and properties
        private readonly NetServer _netServer;

        private readonly ConcurrentDictionary<ushort, ServerPlayerData> _playerData;

        private readonly ServerAddonManager _addonManager;

        private readonly WhiteList _whiteList;
        private readonly AuthorizedList _authorizedList;

        protected readonly Settings.GameSettings GameSettings;
        protected readonly ServerCommandManager CommandManager;

        #endregion

        #region IServerManager properties

        public IReadOnlyCollection<IServerPlayer> Players => _playerData.GetCopy().Values;

        public event Action<IServerPlayer> PlayerConnectEvent;
        public event Action<IServerPlayer> PlayerDisconnectEvent;
        public event Action<IServerPlayer> PlayerEnterSceneEvent;
        public event Action<IServerPlayer> PlayerLeaveSceneEvent;
        
        #endregion

        protected ServerManager(
            NetServer netServer,
            Settings.GameSettings gameSettings,
            PacketManager packetManager
        ) {
            _netServer = netServer;
            GameSettings = gameSettings;
            _playerData = new ConcurrentDictionary<ushort, ServerPlayerData>();

            CommandManager = new ServerCommandManager();

            var serverApi = new ServerApi(this, CommandManager, _netServer);
            _addonManager = new ServerAddonManager(serverApi);

            // Load the whitelist and authorized list from file and write them back again
            _whiteList = WhiteList.LoadFromFile();
            _whiteList.WriteToFile();

            _authorizedList = AuthorizedList.LoadFromFile();
            _authorizedList.WriteToFile();

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
            packetManager.RegisterServerPacketHandler<ChatMessage>(ServerPacketId.ChatMessage, OnChatMessage);

            // Register a timeout handler
            _netServer.ClientTimeoutEvent += OnClientTimeout;

            // Register server shutdown handler
            _netServer.ShutdownEvent += OnServerShutdown;

            // Register a handler for when a client wants to login
            _netServer.LoginRequestEvent += OnLoginRequest;
        }

        #region Internal server manager methods

        public void Initialize() {
            RegisterCommands();
        }

        protected virtual void RegisterCommands() {
            CommandManager.RegisterCommand(new ListCommand(this));
            CommandManager.RegisterCommand(new WhiteListCommand(_whiteList, this));
            CommandManager.RegisterCommand(new AuthorizeCommand(_authorizedList, this));
            CommandManager.RegisterCommand(new AnnounceCommand(_playerData, _netServer));
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

        public void AuthorizeKey(string authKey) {
            _authorizedList.Add(authKey);
        }

        /**
         * Called when the game settings are updated, and need to be broadcast
         */
        public void OnUpdateGameSettings() {
            if (!_netServer.IsStarted) {
                return;
            }

            _netServer.SetDataForAllClients(updateManager => { updateManager.UpdateGameSettings(GameSettings); });
        }

        private void OnHelloServer(ushort id, HelloServer helloServer) {
            Logger.Get().Info(this, $"Received HelloServer data from ID {id}");

            // Start by sending the new client the current Server Settings
            _netServer.GetUpdateManagerForClient(id)?.UpdateGameSettings(GameSettings);

            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Could not find player data for ID: {id}");
                return;
            }

            playerData.CurrentScene = helloServer.SceneName;
            playerData.Position = helloServer.Position;
            playerData.Scale = helloServer.Scale;
            playerData.AnimationId = helloServer.AnimationClipId;

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

            try {
                PlayerConnectEvent?.Invoke(playerData);
            } catch (Exception e) {
                Logger.Get().Warn(this, $"Exception thrown while invoking PlayerConnect event, {e.GetType()}, {e.Message}, {e.StackTrace}");
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
            playerData.Position = playerEnterScene.Position;
            playerData.Scale = playerEnterScene.Scale;
            playerData.AnimationId = playerEnterScene.AnimationClipId;

            OnClientEnterScene(id, playerData);
            
            try {
                PlayerEnterSceneEvent?.Invoke(playerData);
            } catch (Exception e) {
                Logger.Get().Warn(this, $"Exception thrown while invoking PlayerEnterScene event, {e.GetType()}, {e.Message}, {e.StackTrace}");
            }
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
            
            try {
                PlayerLeaveSceneEvent?.Invoke(playerData);
            } catch (Exception e) {
                Logger.Get().Warn(this, $"Exception thrown while invoking PlayerLeaveScene event, {e.GetType()}, {e.Message}, {e.StackTrace}");
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
                if (GameSettings.AlwaysShowMapIcons || GameSettings.OnlyBroadcastMapIconWithWaywardCompass) {
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
            
            try {
                PlayerDisconnectEvent?.Invoke(playerData);
            } catch (Exception e) {
                Logger.Get().Warn(this, $"Exception thrown while invoking PlayerDisconnect event, {e.GetType()}, {e.Message}, {e.StackTrace}");
            }
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

        private bool OnLoginRequest(ushort id, LoginRequest loginRequest, ServerUpdateManager updateManager) {
            Logger.Get().Info(this, $"Received login request from username: {loginRequest.Username}");

            if (_whiteList.IsEnabled) {
                if (!_whiteList.Contains(loginRequest.AuthKey)) {
                    if (!_whiteList.IsPreListed(loginRequest.Username)) {
                        updateManager.SetLoginResponse(new LoginResponse {
                            LoginResponseStatus = LoginResponseStatus.NotWhiteListed
                        });
                        return false;
                    }
                    
                    Logger.Get().Info(this, "  Username was pre-listed, auth key has been added to whitelist");

                    _whiteList.Add(loginRequest.AuthKey);
                    _whiteList.RemovePreList(loginRequest.Username);
                }
            }

            // Check whether the username is not already in use
            foreach (var existingPlayerData in _playerData.GetCopy().Values) {
                if (existingPlayerData.Username.ToLower().Equals(loginRequest.Username.ToLower())) {
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

                if (!correspondingServerAddon.Id.HasValue) {
                    continue;
                }

                // If the addon is also present on the server, we append the addon order with the correct index
                addonOrder.Add(correspondingServerAddon.Id.Value);
            }

            var loginResponse = new LoginResponse {
                LoginResponseStatus = LoginResponseStatus.Success,
                AddonOrder = addonOrder.ToArray()
            };

            updateManager.SetLoginResponse(loginResponse);

            // Create new player data and store it
            var playerData = new ServerPlayerData(
                id, 
                loginRequest.Username, 
                loginRequest.AuthKey,
                _authorizedList
            );
            _playerData[id] = playerData;

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

        public bool TryProcessCommand(ICommandSender commandSender, string message) {
            return CommandManager.ProcessCommand(commandSender, message);
        }

        private void OnChatMessage(ushort id, ChatMessage chatMessage) {
            Logger.Get().Info(this, $"Received chat message from {id}, message: \"{chatMessage.Message}\"");

            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Info(this, $"  Could not process chat message because player data for id {id} is null");
                return;
            }

            if (TryProcessCommand(
                    new PlayerCommandSender(
                        _authorizedList.Contains(playerData.AuthKey), 
                        _netServer.GetUpdateManagerForClient(id)
                    ), 
                    chatMessage.Message
            )) {
                Logger.Get().Info(this, "Chat message was processed as command");
                return;
            }

            var message = $"[{playerData.Username}]: {chatMessage.Message}";
            
            foreach (var idPlayerDataPair in _playerData.GetCopy()) {
                _netServer.GetUpdateManagerForClient(idPlayerDataPair.Key)?.AddChatMessage(message);
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

        private void CheckValidMessage(string message) {
            if (message == null) {
                throw new ArgumentException("Message cannot be null");
            }
            
            if (message.Length > ChatMessage.MaxMessageLength) {
                throw new ArgumentException($"Message length exceeds max length of {ChatMessage.MaxMessageLength}");
            }

            foreach (var messageChar in message) {
                if (!StringUtil.CharByteDict.ContainsFirst(messageChar)) {
                    throw new ArgumentException($"Message contains invalid character: {messageChar}");
                }
            }
        }

        public void SendMessage(ushort id, string message) {
            CheckValidMessage(message);
            
            var updateManager = _netServer.GetUpdateManagerForClient(id);
            updateManager?.AddChatMessage(message);
        }

        public void SendMessage(IServerPlayer player, string message) {
            if (player == null) {
                throw new ArgumentException("Player cannot be null");
            }

            SendMessage(player.Id, message);
        }

        public void BroadcastMessage(string message) {
            CheckValidMessage(message);
        
            foreach (var player in _playerData.GetCopy().Values) {
                var updateManager = _netServer.GetUpdateManagerForClient(player.Id);
                updateManager?.AddChatMessage(message);
            }
        }

        #endregion
    }
}