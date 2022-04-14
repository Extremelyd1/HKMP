using System;
using System.Collections.Generic;
using GlobalEnums;
using Hkmp.Animation;
using Hkmp.Api.Client;
using Hkmp.Eventing;
using Hkmp.Game.Client.Entity;
using Hkmp.Game.Command.Client;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Ui;
using Hkmp.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client {
    /// <summary>
    /// Class that manages the client state (similar to ServerManager).
    /// </summary>
    internal class ClientManager : IClientManager {
        #region Internal client manager variables and properties

        /// <summary>
        /// The net client instance.
        /// </summary>
        private readonly NetClient _netClient;

        /// <summary>
        /// The server manager instance.
        /// </summary>
        private readonly ServerManager _serverManager;

        /// <summary>
        /// The UI manager instance.
        /// </summary>
        private readonly UiManager _uiManager;

        /// <summary>
        /// The current game settings.
        /// </summary>
        private readonly Settings.GameSettings _gameSettings;

        /// <summary>
        /// The loaded mod settings.
        /// </summary>
        private readonly ModSettings _modSettings;

        /// <summary>
        /// The player manager instance.
        /// </summary>
        private readonly PlayerManager _playerManager;

        /// <summary>
        /// The animation manager instance.
        /// </summary>
        private readonly AnimationManager _animationManager;

        /// <summary>
        /// The map manager instance.
        /// </summary>
        private readonly MapManager _mapManager;

        /// <summary>
        /// The entity manager instance.
        /// </summary>
        private readonly EntityManager _entityManager;

        /// <summary>
        /// The client addon manager instance.
        /// </summary>
        private readonly ClientAddonManager _addonManager;

        /// <summary>
        /// The client command manager instance.
        /// </summary>
        private readonly ClientCommandManager _commandManager;

        /// <summary>
        /// Dictionary containing a mapping from user IDs to the client player data.
        /// </summary>
        private readonly Dictionary<ushort, ClientPlayerData> _playerData;

        #endregion

        #region IClientManager properties

        /// <inheritdoc />
        public string Username {
            get {
                if (!_netClient.IsConnected) {
                    throw new Exception("Client is not connected, username is undefined");
                }

                return _username;
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<IClientPlayer> Players => _playerData.Values;

        /// <inheritdoc />
        public event Action ConnectEvent;

        /// <inheritdoc />
        public event Action DisconnectEvent;

        /// <inheritdoc />
        public event Action<IClientPlayer> PlayerConnectEvent;

        /// <inheritdoc />
        public event Action<IClientPlayer> PlayerDisconnectEvent;

        /// <inheritdoc />
        public event Action<IClientPlayer> PlayerEnterSceneEvent;

        /// <inheritdoc />
        public event Action<IClientPlayer> PlayerLeaveSceneEvent;

        /// <inheritdoc />
        public Team Team => _playerManager.LocalPlayerTeam;

        #endregion

        /// <summary>
        /// The username that was last used to connect with.
        /// </summary>
        private string _username;

        /// <summary>
        /// Keeps track of the last updated location of the local player object.
        /// </summary>
        private Vector3 _lastPosition;

        /// <summary>
        /// Keeps track of the last updated scale of the local player object.
        /// </summary>
        private Vector3 _lastScale;

        /// <summary>
        /// Whether the scene has just changed and we are in a scene change.
        /// </summary>
        private bool _sceneChanged;

        /// <summary>
        /// Whether we have already determined whether we are scene host or not for the entity system.
        /// </summary>
        private bool _sceneHostDetermined;

        public ClientManager(
            NetClient netClient,
            ServerManager serverManager,
            PacketManager packetManager,
            UiManager uiManager,
            Settings.GameSettings gameSettings,
            ModSettings modSettings
        ) {
            _netClient = netClient;
            _serverManager = serverManager;
            _uiManager = uiManager;
            _gameSettings = gameSettings;
            _modSettings = modSettings;

            _playerData = new Dictionary<ushort, ClientPlayerData>();

            _playerManager = new PlayerManager(packetManager, gameSettings, _playerData);
            _animationManager = new AnimationManager(netClient, _playerManager, packetManager, gameSettings);
            _mapManager = new MapManager(netClient, gameSettings);

            _entityManager = new EntityManager(netClient);

            new PauseManager(netClient).RegisterHooks();

            _commandManager = new ClientCommandManager();
            var eventAggregator = new EventAggregator();
            RegisterCommands();

            var clientApi = new ClientApi(this, _commandManager, uiManager, netClient, eventAggregator);
            _addonManager = new ClientAddonManager(clientApi);

            ModHooks.FinishedLoadingModsHook += _addonManager.LoadAddons;

            // Check if there is a valid authentication key and if not, generate a new one
            if (!AuthUtil.IsValidAuthKey(modSettings.AuthKey)) {
                modSettings.AuthKey = AuthUtil.GenerateAuthKey();
            }

            // Then authorize the key on the locally hosted server
            serverManager.AuthorizeKey(modSettings.AuthKey);

            // Register packet handlers
            packetManager.RegisterClientPacketHandler<HelloClient>(ClientPacketId.HelloClient, OnHelloClient);
            packetManager.RegisterClientPacketHandler<ServerClientDisconnect>(ClientPacketId.ServerClientDisconnect,
                OnDisconnect);
            packetManager.RegisterClientPacketHandler<PlayerConnect>(ClientPacketId.PlayerConnect, OnPlayerConnect);
            packetManager.RegisterClientPacketHandler<ClientPlayerDisconnect>(ClientPacketId.PlayerDisconnect,
                OnPlayerDisconnect);
            packetManager.RegisterClientPacketHandler<ClientPlayerEnterScene>(ClientPacketId.PlayerEnterScene,
                OnPlayerEnterScene);
            packetManager.RegisterClientPacketHandler<ClientPlayerAlreadyInScene>(ClientPacketId.PlayerAlreadyInScene,
                OnPlayerAlreadyInScene);
            packetManager.RegisterClientPacketHandler<GenericClientData>(ClientPacketId.PlayerLeaveScene,
                OnPlayerLeaveScene);
            packetManager.RegisterClientPacketHandler<PlayerUpdate>(ClientPacketId.PlayerUpdate, OnPlayerUpdate);
            packetManager.RegisterClientPacketHandler<EntityUpdate>(ClientPacketId.EntityUpdate, OnEntityUpdate);
            packetManager.RegisterClientPacketHandler<GameSettingsUpdate>(ClientPacketId.GameSettingsUpdated,
                OnGameSettingsUpdated);
            packetManager.RegisterClientPacketHandler<ChatMessage>(ClientPacketId.ChatMessage, OnChatMessage);

            // Register handlers for events from UI
            uiManager.ConnectInterface.ConnectButtonPressed += Connect;
            uiManager.ConnectInterface.DisconnectButtonPressed += () => Disconnect();
            uiManager.SettingsInterface.OnTeamRadioButtonChange += InternalChangeTeam;
            uiManager.SettingsInterface.OnSkinIdChange += InternalChangeSkin;

            UiManager.InternalChatBox.ChatInputEvent += OnChatInput;

            netClient.ConnectEvent += response => uiManager.OnSuccessfulConnect();
            netClient.ConnectFailedEvent += uiManager.OnFailedConnect;

            // Register the Hero Controller Start, which is when the local player spawns
            On.HeroController.Start += (orig, self) => {
                // Execute the original method
                orig(self);
                // If we are connect to a server, add a username to the player object
                if (netClient.IsConnected) {
                    _playerManager.AddNameToPlayer(
                        HeroController.instance.gameObject,
                        _username,
                        _playerManager.LocalPlayerTeam
                    );
                }
            };

            // Register handlers for scene change and player update
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
            On.HeroController.Update += OnPlayerUpdate;

            // Register client connect and timeout handler
            netClient.ConnectEvent += OnClientConnect;
            netClient.TimeoutEvent += OnTimeout;

            // Register application quit handler
            ModHooks.ApplicationQuitHook += OnApplicationQuit;
        }

        #region Internal client-manager methods

        /// <summary>
        /// Register the default client commands.
        /// </summary>
        private void RegisterCommands() {
            _commandManager.RegisterCommand(new ConnectCommand(this));
            _commandManager.RegisterCommand(new HostCommand(_serverManager));
        }

        /// <summary>
        /// Connect the client to the server with the given address, port and username.
        /// </summary>
        /// <param name="address">The address of the server.</param>
        /// <param name="port">The port of the server.</param>
        /// <param name="username">The username of the client.</param>
        public void Connect(string address, int port, string username) {
            Logger.Get().Info(this, $"Connecting client to server: {address}:{port} as {username}");

            // Stop existing client
            if (_netClient.IsConnected) {
                Logger.Get().Info(this, "Client was already connected, disconnecting first");
                Disconnect();
            }

            // Store username, so we know what to send the server if we are connected
            _username = username;

            // Connect the network client
            _netClient.Connect(
                address,
                port,
                username,
                _modSettings.AuthKey,
                _addonManager.GetNetworkedAddonData()
            );
        }

        /// <summary>
        /// Disconnect the local client from the server.
        /// </summary>
        /// <param name="sendDisconnect">Whether to tell the server we are disconnecting.</param>
        public void Disconnect(bool sendDisconnect = true) {
            if (_netClient.IsConnected) {
                if (sendDisconnect) {
                    // First send the server that we are disconnecting
                    Logger.Get().Info(this, "Sending PlayerDisconnect packet");
                    _netClient.UpdateManager.SetPlayerDisconnect();
                }

                // Then actually disconnect
                _netClient.Disconnect();

                // Let the player manager know we disconnected
                _playerManager.OnDisconnect();

                // Clear the player data dictionary
                _playerData.Clear();

                _uiManager.OnClientDisconnect();

                _addonManager.ClearNetworkedAddonIds();

                // Check whether the game is in the pause menu and reset timescale to 0 in that case
                if (UIManager.instance.uiState.Equals(UIState.PAUSED)) {
                    PauseManager.SetTimeScale(0);
                }

                try {
                    DisconnectEvent?.Invoke();
                } catch (Exception e) {
                    Logger.Get().Warn(this,
                        $"Exception thrown while invoking Disconnect event, {e.GetType()}, {e.Message}, {e.StackTrace}");
                }
            } else {
                Logger.Get().Warn(this, "Could not disconnect client, it was not connected");
            }
        }

        /// <summary>
        /// Callback method for when chat is input by the local user.
        /// </summary>
        /// <param name="message">The message that was submitted by the user.</param>
        private void OnChatInput(string message) {
            if (_commandManager.ProcessCommand(message)) {
                Logger.Get().Info(this, "Chat input was processed as command");
                return;
            }

            if (!_netClient.IsConnected) {
                return;
            }

            _netClient.UpdateManager.SetChatMessage(message);
        }

        /// <summary>
        /// Internal method for changing the local player team.
        /// </summary>
        /// <param name="team">The new team.</param>
        private void InternalChangeTeam(Team team) {
            if (!_netClient.IsConnected) {
                return;
            }

            if (!_gameSettings.TeamsEnabled) {
                Logger.Get().Warn(this, "Team are not enabled by server");
                return;
            }

            if (_playerManager.LocalPlayerTeam == team) {
                return;
            }

            _playerManager.OnLocalPlayerTeamUpdate(team);

            _netClient.UpdateManager.SetTeamUpdate(team);

            UiManager.InternalChatBox.AddMessage($"You are now in Team {team}");
        }

        /// <summary>
        /// Internal method for changing the local player skin.
        /// </summary>
        /// <param name="skinId">The ID of the new skin.</param>
        private void InternalChangeSkin(byte skinId) {
            if (!_netClient.IsConnected) {
                return;
            }

            if (!_gameSettings.AllowSkins) {
                Logger.Get().Warn(this, "User changed skin ID, but skins are not allowed by server");
                return;
            }

            Logger.Get().Info(this, $"Changed local player skin to ID: {skinId}");

            // Let the player manager handle the skin updating and send the change to the server
            _playerManager.UpdateLocalPlayerSkin(skinId);
            _netClient.UpdateManager.SetSkinUpdate(skinId);
        }

        /// <summary>
        /// Callback method for when the net client establishes a connection with a server.
        /// </summary>
        /// <param name="loginResponse">The login response received from the server.</param>
        private void OnClientConnect(LoginResponse loginResponse) {
            // First relay the addon order from the login response to the addon manager
            _addonManager.UpdateNetworkedAddonOrder(loginResponse.AddonOrder);

            // We should only be able to connect during a gameplay scene,
            // which is when the player is spawned already, so we can add the username
            _playerManager.AddNameToPlayer(HeroController.instance.gameObject, _username,
                _playerManager.LocalPlayerTeam);

            Logger.Get().Info(this, "Client is connected, sending Hello packet");

            // If we are in a non-gameplay scene, we transmit that we are not active yet
            var currentSceneName = SceneUtil.GetCurrentSceneName();
            if (SceneUtil.IsNonGameplayScene(currentSceneName)) {
                Logger.Get().Error(this,
                    $"Client connected during a non-gameplay scene named {currentSceneName}, this should never happen!");
                return;
            }

            var transform = HeroController.instance.transform;
            var position = transform.position;

            Logger.Get().Info(this, "Sending Hello packet");

            _netClient.UpdateManager.SetHelloServerData(
                _username,
                SceneUtil.GetCurrentSceneName(),
                new Vector2(position.x, position.y),
                transform.localScale.x > 0,
                (ushort)AnimationManager.GetCurrentAnimationClip()
            );

            // Since we are probably in the pause menu when we connect, set the timescale so the game
            // is running while paused
            PauseManager.SetTimeScale(1.0f);

            UiManager.InternalChatBox.AddMessage("You are connected to the server");

            try {
                ConnectEvent?.Invoke();
            } catch (Exception e) {
                Logger.Get().Warn(this,
                    $"Exception thrown while invoking Connect event, {e.GetType()}, {e.Message}, {e.StackTrace}");
            }
        }

        /// <summary>
        /// Callback method for when we receive the HelloClient data.
        /// </summary>
        /// <param name="helloClient">The HelloClient packet data.</param>
        private void OnHelloClient(HelloClient helloClient) {
            Logger.Get().Info(this, "Received HelloClient from server");

            // Fill the player data dictionary with the info from the packet
            foreach (var (id, username) in helloClient.ClientInfo) {
                _playerData[id] = new ClientPlayerData(id, username);
            }
        }

        /// <summary>
        /// Callback method for when we receive a server disconnect.
        /// </summary>
        private void OnDisconnect(ServerClientDisconnect disconnect) {
            Logger.Get().Info(this, $"Received ServerClientDisconnect, reason: {disconnect.Reason}");

            if (disconnect.Reason == DisconnectReason.Banned) {
                UiManager.InternalChatBox.AddMessage("You are banned from the server");
            } else if (disconnect.Reason == DisconnectReason.Kicked) {
                UiManager.InternalChatBox.AddMessage("You are kicked from the server");
            } else if (disconnect.Reason == DisconnectReason.Shutdown) {
                UiManager.InternalChatBox.AddMessage("You are disconnected from the server (server is shutting down)");
            }

            // Disconnect without sending the server that we disconnect, because the server knows that already
            Disconnect(false);
        }

        /// <summary>
        /// Callback method for when a player connects to the server.
        /// </summary>
        /// <param name="playerConnect">The PlayerConnect packet data.</param>
        private void OnPlayerConnect(PlayerConnect playerConnect) {
            Logger.Get().Info(this, $"Received PlayerConnect data for ID: {playerConnect.Id}");

            var playerData = new ClientPlayerData(playerConnect.Id, playerConnect.Username);
            _playerData[playerConnect.Id] = playerData;

            UiManager.InternalChatBox.AddMessage($"Player '{playerConnect.Username}' connected to the server");

            try {
                PlayerConnectEvent?.Invoke(playerData);
            } catch (Exception e) {
                Logger.Get().Warn(this,
                    $"Exception thrown while invoking PlayerConnect event, {e.GetType()}, {e.Message}, {e.StackTrace}");
            }
        }

        /// <summary>
        /// Callback method for when a player disconnects from the server.
        /// </summary>
        /// <param name="playerDisconnect">The ClientPlayerDisconnect packet data.</param>
        private void OnPlayerDisconnect(ClientPlayerDisconnect playerDisconnect) {
            var id = playerDisconnect.Id;
            var username = playerDisconnect.Username;

            Logger.Get().Info(this,
                $"Received PlayerDisconnect data for ID: {id}, timed out: {playerDisconnect.TimedOut}");

            // Destroy player object
            _playerManager.DestroyPlayer(id);

            // Destroy map icon
            _mapManager.RemovePlayerIcon(id);

            // Store a reference of the player data before removing it to pass to the API event
            _playerData.TryGetValue(id, out var playerData);

            // Clear the player from the player data mapping
            _playerData.Remove(id);

            if (playerDisconnect.TimedOut) {
                UiManager.InternalChatBox.AddMessage($"Player '{username}' timed out");
            } else {
                UiManager.InternalChatBox.AddMessage($"Player '{username}' disconnected from the server");
            }

            try {
                PlayerDisconnectEvent?.Invoke(playerData);
            } catch (Exception e) {
                Logger.Get().Warn(this,
                    $"Exception thrown while invoking PlayerDisconnect event, {e.GetType()}, {e.Message}, {e.StackTrace}");
            }
        }

        /// <summary>
        /// Callback method for when we receive that a player is already in the scene we are entering.
        /// </summary>
        /// <param name="alreadyInScene">The ClientPlayerAlreadyInScene packet data.</param>
        private void OnPlayerAlreadyInScene(ClientPlayerAlreadyInScene alreadyInScene) {
            Logger.Get().Info(this, "Received AlreadyInScene packet");

            foreach (var playerEnterScene in alreadyInScene.PlayerEnterSceneList) {
                Logger.Get().Info(this, $"Updating already in scene player with ID: {playerEnterScene.Id}");
                OnPlayerEnterScene(playerEnterScene);
            }

            if (alreadyInScene.SceneHost) {
                // Notify the entity manager that we are scene host
                _entityManager.OnBecomeSceneHost();
            } else {
                // Notify the entity manager that we are scene client (non-host)
                _entityManager.OnBecomeSceneClient();
            }

            // Whether there were players in the scene or not, we have now determined whether
            // we are the scene host
            _sceneHostDetermined = true;
        }

        /// <summary>
        /// Callback method for when another player enters our scene.
        /// </summary>
        /// <param name="enterSceneData">The ClientPlayerEnterScene packet data.</param>
        private void OnPlayerEnterScene(ClientPlayerEnterScene enterSceneData) {
            // Read ID from player data
            var id = enterSceneData.Id;

            Logger.Get().Info(this, $"Player {id} entered scene");

            if (!_playerData.TryGetValue(id, out var playerData)) {
                playerData = new ClientPlayerData(id, enterSceneData.Username);
                _playerData[id] = playerData;
            }

            playerData.IsInLocalScene = true;

            _playerManager.SpawnPlayer(
                playerData,
                enterSceneData.Username,
                enterSceneData.Position,
                enterSceneData.Scale,
                enterSceneData.Team,
                enterSceneData.SkinId
            );
            _animationManager.UpdatePlayerAnimation(id, enterSceneData.AnimationClipId, 0);

            try {
                PlayerEnterSceneEvent?.Invoke(playerData);
            } catch (Exception e) {
                Logger.Get().Warn(this,
                    $"Exception thrown while invoking PlayerEnterScene event, {e.GetType()}, {e.Message}, {e.StackTrace}");
            }
        }

        /// <summary>
        /// Callback method for when a player leaves our scene.
        /// </summary>
        /// <param name="data">The generic client packet data.</param>
        private void OnPlayerLeaveScene(GenericClientData data) {
            var id = data.Id;

            Logger.Get().Info(this, $"Player {id} left scene");

            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Could not find player data for player with ID {id}");
                return;
            }

            // Destroy corresponding player
            _playerManager.DestroyPlayer(id);

            playerData.IsInLocalScene = false;

            try {
                PlayerLeaveSceneEvent?.Invoke(playerData);
            } catch (Exception e) {
                Logger.Get().Warn(this,
                    $"Exception thrown while invoking PlayerLeaveScene event, {e.GetType()}, {e.Message}, {e.StackTrace}");
            }
        }

        /// <summary>
        /// Callback method for when a player update is received.
        /// </summary>
        /// <param name="playerUpdate">The PlayerUpdate packet data.</param>
        private void OnPlayerUpdate(PlayerUpdate playerUpdate) {
            // Update the values of the player objects in the packet
            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Position)) {
                _playerManager.UpdatePosition(playerUpdate.Id, playerUpdate.Position);
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Scale)) {
                _playerManager.UpdateScale(playerUpdate.Id, playerUpdate.Scale);
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.MapPosition)) {
                _mapManager.OnPlayerMapUpdate(playerUpdate.Id, playerUpdate.MapPosition);
            }

            if (playerUpdate.UpdateTypes.Contains(PlayerUpdateType.Animation)) {
                foreach (var animationInfo in playerUpdate.AnimationInfos) {
                    _animationManager.OnPlayerAnimationUpdate(
                        playerUpdate.Id,
                        animationInfo.ClipId,
                        animationInfo.Frame,
                        animationInfo.EffectInfo
                    );
                }
            }
        }

        /// <summary>
        /// Callback method for when an entity update is received.
        /// </summary>
        /// <param name="entityUpdate">The EntityUpdate packet data.</param>
        private void OnEntityUpdate(EntityUpdate entityUpdate) {
            // We only propagate entity updates to the entity manager if we have determined the scene host
            if (!_sceneHostDetermined) {
                return;
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Position)) {
                _entityManager.UpdateEntityPosition((EntityType)entityUpdate.EntityType, entityUpdate.Id,
                    entityUpdate.Position);
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.State)) {
                List<byte> variables;

                if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Variables)) {
                    variables = entityUpdate.Variables;
                } else {
                    variables = new List<byte>();
                }

                _entityManager.UpdateEntityState(
                    (EntityType)entityUpdate.EntityType,
                    entityUpdate.Id,
                    entityUpdate.State,
                    variables
                );
            }
        }

        /// <summary>
        /// Callback method for when the game settings are updated by the server.
        /// </summary>
        /// <param name="update">The GameSettingsUpdate packet data.</param>
        private void OnGameSettingsUpdated(GameSettingsUpdate update) {
            var pvpChanged = false;
            var bodyDamageChanged = false;
            var displayNamesChanged = false;
            var alwaysShowMapChanged = false;
            var onlyCompassChanged = false;
            var teamsChanged = false;
            var allowSkinsChanged = false;

            // Check whether the PvP state changed
            if (_gameSettings.IsPvpEnabled != update.GameSettings.IsPvpEnabled) {
                pvpChanged = true;

                var message = $"PvP is now {(update.GameSettings.IsPvpEnabled ? "enabled" : "disabled")}";

                UiManager.InternalChatBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether the body damage state changed
            if (_gameSettings.IsBodyDamageEnabled != update.GameSettings.IsBodyDamageEnabled) {
                bodyDamageChanged = true;

                var message =
                    $"Body damage is now {(update.GameSettings.IsBodyDamageEnabled ? "enabled" : "disabled")}";

                UiManager.InternalChatBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether the always show map icons state changed
            if (_gameSettings.AlwaysShowMapIcons != update.GameSettings.AlwaysShowMapIcons) {
                alwaysShowMapChanged = true;

                var message =
                    $"Map icons are now{(update.GameSettings.AlwaysShowMapIcons ? "" : " not")} always visible";

                UiManager.InternalChatBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether the wayward compass broadcast state changed
            if (_gameSettings.OnlyBroadcastMapIconWithWaywardCompass !=
                update.GameSettings.OnlyBroadcastMapIconWithWaywardCompass) {
                onlyCompassChanged = true;

                var message =
                    $"Map icons are {(update.GameSettings.OnlyBroadcastMapIconWithWaywardCompass ? "now only" : "not")} broadcast when wearing the Wayward Compass charm";

                UiManager.InternalChatBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether the display names setting changed
            if (_gameSettings.DisplayNames != update.GameSettings.DisplayNames) {
                displayNamesChanged = true;

                var message = $"Names are {(update.GameSettings.DisplayNames ? "now" : "no longer")} displayed";

                UiManager.InternalChatBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether the teams enabled setting changed
            if (_gameSettings.TeamsEnabled != update.GameSettings.TeamsEnabled) {
                teamsChanged = true;

                var message = $"Teams are {(update.GameSettings.TeamsEnabled ? "now" : "no longer")} enabled";

                UiManager.InternalChatBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether allow skins setting changed
            if (_gameSettings.AllowSkins != update.GameSettings.AllowSkins) {
                allowSkinsChanged = true;

                var message = $"Skins are {(update.GameSettings.AllowSkins ? "now" : "no longer")} enabled";

                UiManager.InternalChatBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Update the settings so callbacks can read updated values
            _gameSettings.SetAllProperties(update.GameSettings);

            // Only update the player manager if the either PvP or body damage have been changed
            if (pvpChanged || bodyDamageChanged || displayNamesChanged) {
                _playerManager.OnGameSettingsUpdated(pvpChanged || bodyDamageChanged, displayNamesChanged);
            }

            if (alwaysShowMapChanged || onlyCompassChanged) {
                if (!_gameSettings.AlwaysShowMapIcons && !_gameSettings.OnlyBroadcastMapIconWithWaywardCompass) {
                    _mapManager.RemoveAllIcons();
                }
            }

            // If the teams setting changed, we invoke the registered event handler if they exist
            if (teamsChanged) {
                // If the team setting was disabled, we reset all teams 
                if (!_gameSettings.TeamsEnabled) {
                    _playerManager.ResetAllTeams();
                }

                _uiManager.OnTeamSettingChange();
            }

            // If the allow skins setting changed and it is no longer allowed, we reset all existing skins
            if (allowSkinsChanged && !_gameSettings.AllowSkins) {
                _playerManager.ResetAllPlayerSkins();
            }
        }

        /// <summary>
        /// Callback method for when the Unity scene changes.
        /// </summary>
        /// <param name="oldScene">The old scene instance.</param>
        /// <param name="newScene">The new scene instance.</param>
        private void OnSceneChange(Scene oldScene, Scene newScene) {
            Logger.Get().Info(this, $"Scene changed from {oldScene.name} to {newScene.name}");

            // Always destroy existing players, because we changed scenes
            _playerManager.DestroyAllPlayers();

            // For each known player set that they are not in our scene anymore
            foreach (var playerData in _playerData.Values) {
                playerData.IsInLocalScene = false;
            }

            // If we are not connected, there is nothing to send to
            if (!_netClient.IsConnected) {
                return;
            }

            _sceneChanged = true;

            // Reset the status of whether we determined the scene host or not
            _sceneHostDetermined = false;

            // Ignore scene changes from and to non-gameplay scenes
            if (SceneUtil.IsNonGameplayScene(oldScene.name) && SceneUtil.IsNonGameplayScene(newScene.name)) {
                return;
            }

            _netClient.UpdateManager.SetLeftScene();
        }

        /// <summary>
        /// Callback method on the HeroController#Update method.
        /// </summary>
        /// <param name="orig">The original method.</param>
        /// <param name="self">The HeroController instance.</param>
        private void OnPlayerUpdate(On.HeroController.orig_Update orig, HeroController self) {
            // Make sure the original method executes
            orig(self);

            // Ignore player position updates on non-gameplay scenes
            var currentSceneName = SceneUtil.GetCurrentSceneName();
            if (SceneUtil.IsNonGameplayScene(currentSceneName)) {
                return;
            }

            // If we are not connected, there is nothing to send to
            if (!_netClient.IsConnected) {
                return;
            }

            var heroTransform = HeroController.instance.transform;

            var newPosition = heroTransform.position;
            // If the position changed since last check
            if (newPosition != _lastPosition) {
                // Update the last position, since it changed
                _lastPosition = newPosition;

                if (_sceneChanged) {
                    _sceneChanged = false;


                    // Set some default values for the packet variables in case we don't have a HeroController instance
                    // This might happen when we are in a non-gameplay scene without the knight
                    var position = Vector2.Zero;
                    var scale = Vector3.zero;
                    ushort animationClipId = 0;

                    // If we do have a HeroController instance, use its values
                    if (HeroController.instance != null) {
                        var transform = HeroController.instance.transform;
                        var transformPos = transform.position;

                        position = new Vector2(transformPos.x, transformPos.y);
                        scale = transform.localScale;
                        animationClipId = (ushort)AnimationManager.GetCurrentAnimationClip();
                    }

                    Logger.Get().Info(this, "Sending EnterScene packet");

                    _netClient.UpdateManager.SetEnterSceneData(
                        SceneUtil.GetCurrentSceneName(),
                        position,
                        scale.x > 0,
                        animationClipId
                    );
                } else {
                    // If this was not the first position update after a scene change,
                    // we can simply send a position update packet
                    _netClient.UpdateManager.UpdatePlayerPosition(new Vector2(newPosition.x, newPosition.y));
                }
            }

            var newScale = heroTransform.localScale;
            // If the scale changed since last check
            if (newScale != _lastScale) {
                _netClient.UpdateManager.UpdatePlayerScale(newScale.x > 0);

                // Update the last scale, since it changed
                _lastScale = newScale;
            }
        }

        /// <summary>
        /// Callback method for when a chat message is received.
        /// </summary>
        /// <param name="chatMessage">The ChatMessage packet data.</param>
        private void OnChatMessage(ChatMessage chatMessage) {
            UiManager.InternalChatBox.AddMessage(chatMessage.Message);
        }

        /// <summary>
        /// Callback method for when the net client is timed out.
        /// </summary>
        private void OnTimeout() {
            if (!_netClient.IsConnected) {
                return;
            }

            Logger.Get().Info(this, "Connection to server timed out, disconnecting");
            UiManager.InternalChatBox.AddMessage("You are disconnected from the server (server timed out)");

            Disconnect();
        }

        /// <summary>
        /// Callback method for when the local user quits the application.
        /// </summary>
        private void OnApplicationQuit() {
            if (!_netClient.IsConnected) {
                return;
            }

            // Send a disconnect packet before exiting the application
            Logger.Get().Info(this, "Sending PlayerDisconnect packet");
            _netClient.UpdateManager.SetPlayerDisconnect();
            _netClient.Disconnect();
        }

        #endregion

        #region IClientManager methods

        /// <inheritdoc />
        public IClientPlayer GetPlayer(ushort id) {
            return TryGetPlayer(id, out var player) ? player : null;
        }

        /// <inheritdoc />
        public bool TryGetPlayer(ushort id, out IClientPlayer player) {
            var found = _playerData.TryGetValue(id, out var playerData);
            player = playerData;

            return found;
        }

        /// <inheritdoc />
        public void ChangeTeam(Team team) {
            if (!_netClient.IsConnected) {
                throw new InvalidOperationException("Client is not connected, cannot change team");
            }

            InternalChangeTeam(team);
        }

        /// <inheritdoc />
        public void ChangeSkin(byte skinId) {
            if (!_netClient.IsConnected) {
                throw new InvalidOperationException("Client is not connected, cannot change skin");
            }

            InternalChangeSkin(skinId);
        }

        #endregion
    }
}