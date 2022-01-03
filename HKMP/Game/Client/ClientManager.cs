using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GlobalEnums;
using Hkmp.Animation;
using Hkmp.Game.Client.Entity;
using Hkmp.Networking;
using Hkmp.Networking.Client;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client {
    /**
     * Class that manages the client state (similar to ServerManager).
     * For example keeping track of spawning/destroying player objects.
     */
    public class ClientManager {
        private readonly NetClient _netClient;
        private readonly PlayerManager _playerManager;
        private readonly AnimationManager _animationManager;
        private readonly MapManager _mapManager;
        private readonly Game.Settings.GameSettings _gameSettings;

        private readonly EntityManager _entityManager;

        // The username that was used to connect with
        private string _username;

        // Keeps track of the last updated location of the local player object
        private Vector3 _lastPosition;

        // Keeps track of the last updated scale of the local player object
        private Vector3 _lastScale;

        // Whether we are currently in a scene change
        private bool _sceneChanged;

        // Whether we have already determined whether we are scene host or not
        private bool _sceneHostDetermined;

        private event Action TeamSettingChangeEvent;

        //private event Action ServerKnightChangeEvent;

        public ClientManager(
            NetworkManager networkManager,
            PlayerManager playerManager,
            AnimationManager animationManager,
            MapManager mapManager,
            Game.Settings.GameSettings gameSettings,
            PacketManager packetManager
        ) {
            _netClient = networkManager.GetNetClient();
            _playerManager = playerManager;
            _animationManager = animationManager;
            _mapManager = mapManager;
            _gameSettings = gameSettings;

            _entityManager = new EntityManager(_netClient);

            new PauseManager(_netClient).RegisterHooks();

            // Register packet handlers
            packetManager.RegisterClientPacketHandler(ClientPacketId.ServerShutdown, OnServerShutdown);
            packetManager.RegisterClientPacketHandler<PlayerConnect>(ClientPacketId.PlayerConnect, OnPlayerConnect);
            packetManager.RegisterClientPacketHandler<ClientPlayerDisconnect>(ClientPacketId.PlayerDisconnect,
                OnPlayerDisconnect);
            packetManager.RegisterClientPacketHandler<ClientPlayerEnterScene>(ClientPacketId.PlayerEnterScene,
                OnPlayerEnterScene);
            packetManager.RegisterClientPacketHandler<ClientAlreadyInScene>(ClientPacketId.AlreadyInScene,
                OnAlreadyInScene);
            packetManager.RegisterClientPacketHandler<ClientPlayerLeaveScene>(ClientPacketId.PlayerLeaveScene,
                OnPlayerLeaveScene);
            packetManager.RegisterClientPacketHandler<PlayerUpdate>(ClientPacketId.PlayerUpdate, OnPlayerUpdate);
            packetManager.RegisterClientPacketHandler<EntityUpdate>(ClientPacketId.EntityUpdate, OnEntityUpdate);
            packetManager.RegisterClientPacketHandler<GameSettingsUpdate>(ClientPacketId.GameSettingsUpdated,
                OnGameSettingsUpdated);

            // Register the Hero Controller Start, which is when the local player spawns
            On.HeroController.Start += (orig, self) => {
                // Execute the original method
                orig(self);
                // If we are connect to a server, add a username to the player object
                if (networkManager.GetNetClient().IsConnected) {
                    _playerManager.AddNameToPlayer(HeroController.instance.gameObject, _username,
                        _playerManager.LocalPlayerTeam);
                }
            };

            // Register handlers for scene change and player update
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
            On.HeroController.Update += OnPlayerUpdate;

            // Register client connect handler
            _netClient.RegisterOnConnect(OnClientConnect);

            _netClient.RegisterOnTimeout(OnTimeout);

            // Register application quit handler
            ModHooks.ApplicationQuitHook += OnApplicationQuit;
        }

        /**
         * Connect the client with the server with the given address and port
         * and use the given username
         */
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
            _netClient.Connect(address, port, username);
        }

        /**
         * Disconnect the local client from the server
         */
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

                // Check whether the game is in the pause menu and reset timescale to 0 in that case
                if (UIManager.instance.uiState.Equals(UIState.PAUSED)) {
                    PauseManager.SetTimeScale(0);
                }

                Ui.UiManager.InfoBox.AddMessage("You are disconnected from the server");
            } else {
                Logger.Get().Warn(this, "Could not disconnect client, it was not connected");
            }
        }

        public void RegisterOnConnect(Action onConnect) {
            _netClient.RegisterOnConnect(onConnect);
        }

        public void RegisterOnConnectFailed(Action onConnectFailed) {
            _netClient.RegisterOnConnectFailed(onConnectFailed);
        }

        public void RegisterOnDisconnect(Action onDisconnect) {
            _netClient.RegisterOnDisconnect(onDisconnect);
        }

        public void RegisterTeamSettingChange(Action onTeamSettingChange) {
            TeamSettingChangeEvent += onTeamSettingChange;
        }

        public void ChangeTeam(Team team) {
            if (!_netClient.IsConnected) {
                return;
            }

            _playerManager.OnLocalPlayerTeamUpdate(team);

            _netClient.UpdateManager.SetTeamUpdate(team);

            Ui.UiManager.InfoBox.AddMessage($"You are now in Team {team}");
        }

        public void ChangeSkin(byte skinId) {
            if (!_netClient.IsConnected) {
                return;
            }

            if (!_gameSettings.AllowSkins) {
                Logger.Get().Info(this, "User changed skin ID, but skins are not allowed by server");
                return;
            }

            Logger.Get().Info(this, $"Changed local player skin to ID: {skinId}");

            // Let the player manager handle the skin updating and send the change to the server
            _playerManager.UpdateLocalPlayerSkin(skinId);
            _netClient.UpdateManager.SetSkinUpdate(skinId);
        }

        private void OnClientConnect() {
            // We should only be able to connect during a gameplay scene,
            // which is when the player is spawned already, so we can add the username
            ThreadUtil.RunActionOnMainThread(() => {
                _playerManager.AddNameToPlayer(HeroController.instance.gameObject, _username,
                    _playerManager.LocalPlayerTeam);
            });

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
                new Math.Vector2(position.x, position.y),
                transform.localScale.x > 0,
                (ushort) _animationManager.GetCurrentAnimationClip()
            );

            // Since we are probably in the pause menu when we connect, set the timescale so the game
            // is running while paused
            PauseManager.SetTimeScale(1.0f);

            Ui.UiManager.InfoBox.AddMessage("You are connected to the server");
        }

        private void OnServerShutdown() {
            Logger.Get().Info(this, "Server is shutting down, clearing players and disconnecting client");

            // Disconnect without sending the server that we disconnect, because the server is shutting down anyway
            Disconnect(false);
        }

        private void OnPlayerConnect(PlayerConnect playerConnect) {
            Logger.Get().Info(this, $"Received PlayerConnect data for ID: {playerConnect.Id}");

            Ui.UiManager.InfoBox.AddMessage($"Player '{playerConnect.Username}' connected to the server");
        }

        private void OnPlayerDisconnect(ClientPlayerDisconnect playerDisconnect) {
            var id = playerDisconnect.Id;
            var username = playerDisconnect.Username;

            Logger.Get().Info(this, $"Received PlayerDisconnect data for ID: {id}, timed out: {playerDisconnect.TimedOut}");

            // Destroy player object
            _playerManager.DestroyPlayer(id);

            // Destroy map icon
            _mapManager.RemovePlayerIcon(id);

            if (playerDisconnect.TimedOut) {
                Ui.UiManager.InfoBox.AddMessage($"Player '{username}' timed out");
            } else {
                Ui.UiManager.InfoBox.AddMessage($"Player '{username}' disconnected from the server");
            }
            
            // If we became scene host due to this player leaving, we need to notify the entity manager
            if (playerDisconnect.SceneHost) {
                _entityManager.OnSwitchToSceneHost();
            }
        }

        private void OnAlreadyInScene(ClientAlreadyInScene alreadyInScene) {
            Logger.Get().Info(this, "Received AlreadyInScene packet");

            foreach (var playerEnterScene in alreadyInScene.PlayerEnterSceneList) {
                Logger.Get().Info(this, $"Updating already in scene player with ID: {playerEnterScene.Id}");
                OnPlayerEnterScene(playerEnterScene);
            }

            if (alreadyInScene.SceneHost) {
                // Notify the entity manager that we are scene host
                _entityManager.OnEnterSceneAsHost();
            } else {
                // Notify the entity manager that we are scene client (non-host) with the list of entity updates
                _entityManager.OnEnterSceneAsClient(alreadyInScene.EntityUpdates);
            }

            // Whether there were players in the scene or not, we have now determined whether
            // we are the scene host
            _sceneHostDetermined = true;
        }

        private void OnPlayerEnterScene(ClientPlayerEnterScene playerData) {
            // Read ID from player data
            var id = playerData.Id;

            Logger.Get().Info(this, $"Player {id} entered scene, spawning player");

            _playerManager.SpawnPlayer(
                id,
                playerData.Username,
                playerData.Position,
                playerData.Scale,
                playerData.Team,
                playerData.SkinId
            );
            _animationManager.UpdatePlayerAnimation(id, playerData.AnimationClipId, 0);
        }

        private void OnPlayerLeaveScene(ClientPlayerLeaveScene playerLeaveData) {
            Logger.Get().Info(this, $"Player {playerLeaveData.Id} left scene, destroying player");
            
            // Destroy corresponding player
            _playerManager.DestroyPlayer(playerLeaveData.Id);

            // If we became scene host due to this player leaving, we need to notify the entity manager
            if (playerLeaveData.SceneHost) {
                _entityManager.OnSwitchToSceneHost();
            }
        }

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

        private void OnEntityUpdate(EntityUpdate entityUpdate) {
            // We only propagate entity updates to the entity manager if we have determined the scene host
            if (!_sceneHostDetermined) {
                return;
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Position)) {
                _entityManager.UpdateEntityPosition(
                    (EntityType) entityUpdate.EntityType,
                    entityUpdate.Id,
                    entityUpdate.Position
                );
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Scale)) {
                _entityManager.UpdateEntityScale(
                    (EntityType) entityUpdate.EntityType, 
                    entityUpdate.Id,
                    entityUpdate.Scale
                );
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.Animation)) {
                foreach (var animation in entityUpdate.AnimationInfos) {
                    _entityManager.UpdateEntityAnimation(
                        (EntityType) entityUpdate.EntityType,
                        entityUpdate.Id,
                        animation.AnimationIndex,
                        animation.AnimationInfo
                    );
                }
            }

            if (entityUpdate.UpdateTypes.Contains(EntityUpdateType.State)) {
                _entityManager.UpdateEntityState(
                    (EntityType) entityUpdate.EntityType,
                    entityUpdate.Id,
                    entityUpdate.State
                );
            }
        }

        private void OnGameSettingsUpdated(GameSettingsUpdate update) {
            var pvpChanged = false;
            var bodyDamageChanged = false;
            var displayNamesChanged = false;
            var alwaysShowMapChanged = false;
            var onlyCompassChanged = false;
            var teamsChanged = false;
            var allowSkinsChanged = false;
            var syncEntitiesChanged = false;

            // Check whether the PvP state changed
            if (_gameSettings.IsPvpEnabled != update.GameSettings.IsPvpEnabled) {
                pvpChanged = true;

                var message = $"PvP is now {(update.GameSettings.IsPvpEnabled ? "enabled" : "disabled")}";

                Ui.UiManager.InfoBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether the body damage state changed
            if (_gameSettings.IsBodyDamageEnabled != update.GameSettings.IsBodyDamageEnabled) {
                bodyDamageChanged = true;

                var message =
                    $"Body damage is now {(update.GameSettings.IsBodyDamageEnabled ? "enabled" : "disabled")}";

                Ui.UiManager.InfoBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether the always show map icons state changed
            if (_gameSettings.AlwaysShowMapIcons != update.GameSettings.AlwaysShowMapIcons) {
                alwaysShowMapChanged = true;

                var message =
                    $"Map icons are now{(update.GameSettings.AlwaysShowMapIcons ? "" : " not")} always visible";

                Ui.UiManager.InfoBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether the wayward compass broadcast state changed
            if (_gameSettings.OnlyBroadcastMapIconWithWaywardCompass !=
                update.GameSettings.OnlyBroadcastMapIconWithWaywardCompass) {
                onlyCompassChanged = true;

                var message =
                    $"Map icons are {(update.GameSettings.OnlyBroadcastMapIconWithWaywardCompass ? "now only" : "not")} broadcast when wearing the Wayward Compass charm";

                Ui.UiManager.InfoBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether the display names setting changed
            if (_gameSettings.DisplayNames != update.GameSettings.DisplayNames) {
                displayNamesChanged = true;

                var message = $"Names are {(update.GameSettings.DisplayNames ? "now" : "no longer")} displayed";

                Ui.UiManager.InfoBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether the teams enabled setting changed
            if (_gameSettings.TeamsEnabled != update.GameSettings.TeamsEnabled) {
                teamsChanged = true;

                var message = $"Teams are {(update.GameSettings.TeamsEnabled ? "now" : "no longer")} enabled";

                Ui.UiManager.InfoBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            // Check whether allow skins setting changed
            if (_gameSettings.AllowSkins != update.GameSettings.AllowSkins) {
                allowSkinsChanged = true;

                var message = $"Skins are {(update.GameSettings.AllowSkins ? "now" : "no longer")} enabled";

                Ui.UiManager.InfoBox.AddMessage(message);
                Logger.Get().Info(this, message);
            }

            if (_gameSettings.SyncEntities != update.GameSettings.SyncEntities) {
                syncEntitiesChanged = true;
                
                var message = $"Entities are {(update.GameSettings.AllowSkins ? "now" : "no longer")} synced";

                Ui.UiManager.InfoBox.AddMessage(message);
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

                TeamSettingChangeEvent?.Invoke();
            }

            // If the allow skins setting changed and it is no longer allowed, we reset all existing skins
            if (allowSkinsChanged && !_gameSettings.AllowSkins) {
                _playerManager.ResetAllPlayerSkins();
            }

            // If the sync entities setting changed, inform the entity manager
            if (syncEntitiesChanged) {
                _entityManager.OnEntitySyncSettingChanged(_gameSettings.SyncEntities);
            }
        }

        private void OnSceneChange(Scene oldScene, Scene newScene) {
            Logger.Get().Info(this, $"Scene changed from {oldScene.name} to {newScene.name}");

            // Always destroy existing players, because we changed scenes
            _playerManager.DestroyAllPlayers();

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
                    var position = Math.Vector2.Zero;
                    var scale = Vector3.zero;
                    ushort animationClipId = 0;

                    // If we do have a HeroController instance, use its values
                    if (HeroController.instance != null) {
                        var transform = HeroController.instance.transform;
                        var transformPos = transform.position;

                        position = new Math.Vector2(transformPos.x, transformPos.y);
                        scale = transform.localScale;
                        animationClipId = (ushort) _animationManager.GetCurrentAnimationClip();
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
                    _netClient.UpdateManager.UpdatePlayerPosition(new Math.Vector2(newPosition.x, newPosition.y));
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

        private void OnTimeout() {
            if (!_netClient.IsConnected) {
                return;
            }

            Logger.Get().Info(this, "Connection to server timed out, disconnecting");
            
            Disconnect();
        }

        private void OnApplicationQuit() {
            if (!_netClient.IsConnected) {
                return;
            }

            // Send a disconnect packet before exiting the application
            Logger.Get().Info(this, "Sending PlayerDisconnect packet");
            _netClient.UpdateManager.SetPlayerDisconnect();
            _netClient.Disconnect();
        }
    }
}