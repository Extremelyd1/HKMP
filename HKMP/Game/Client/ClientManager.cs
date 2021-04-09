using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GlobalEnums;
using HKMP.Animation;
using HKMP.Game.Client.Entity;
using HKMP.Networking;
using HKMP.Networking.Client;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Data;
using HKMP.Util;
using HKMP.ServerKnights;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HKMP.Game.Client {
    /**
     * Class that manages the client state (similar to ServerManager).
     * For example keeping track of spawning/destroying player objects.
     */
    public class ClientManager {
        // How long to wait before disconnecting from the server after not receiving a heart beat
        private const int ConnectionTimeout = 50000;
        
        private readonly NetClient _netClient;
        private readonly PlayerManager _playerManager;
        private readonly ServerKnightsManager _serverKnightsManager;
        private readonly AnimationManager _animationManager;
        private readonly MapManager _mapManager;
        private readonly Settings.GameSettings _gameSettings;

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

        // Stopwatch to keep track of the time since the last server packet
        private readonly Stopwatch _heartBeatReceiveStopwatch;

        private event Action TeamSettingChangeEvent;

        //private event Action ServerKnightChangeEvent;

        public ClientManager(NetworkManager networkManager, PlayerManager playerManager,
            AnimationManager animationManager, MapManager mapManager, Settings.GameSettings gameSettings,
            PacketManager packetManager,ServerKnightsManager serverKnightsManager)
        {
            _netClient = networkManager.GetNetClient();
            _serverKnightsManager = serverKnightsManager;;
            _playerManager = playerManager;
            _animationManager = animationManager;
            _mapManager = mapManager;
            _gameSettings = gameSettings;

            _entityManager = new EntityManager(_netClient);
            
            _heartBeatReceiveStopwatch = new Stopwatch();

            // Register packet handlers
            packetManager.RegisterClientPacketHandler(ClientPacketId.ServerShutdown, OnServerShutdown);
            packetManager.RegisterClientPacketHandler<PlayerConnect>(ClientPacketId.PlayerConnect, OnPlayerConnect);
            packetManager.RegisterClientPacketHandler<ClientPlayerDisconnect>(ClientPacketId.PlayerDisconnect, OnPlayerDisconnect);
            packetManager.RegisterClientPacketHandler<ClientPlayerEnterScene>(ClientPacketId.PlayerEnterScene,
                OnPlayerEnterScene);
            packetManager.RegisterClientPacketHandler<ClientPlayerAlreadyInScene>(ClientPacketId.PlayerAlreadyInScene, OnPlayerAlreadyInScene);
            packetManager.RegisterClientPacketHandler<GenericClientData>(ClientPacketId.PlayerLeaveScene,
                OnPlayerLeaveScene);
            packetManager.RegisterClientPacketHandler<PlayerUpdate>(ClientPacketId.PlayerUpdate, OnPlayerUpdate);
            packetManager.RegisterClientPacketHandler<EntityUpdate>(ClientPacketId.EntityUpdate, OnEntityUpdate);
            packetManager.RegisterClientPacketHandler<ClientPlayerTeamUpdate>(ClientPacketId.PlayerTeamUpdate, OnPlayerTeamUpdate);
            packetManager.RegisterClientPacketHandler<ClientServerKnightUpdate>(ClientPacketId.ServerKnightUpdate, OnServerKnightUpdate);
            packetManager.RegisterClientPacketHandler<GameSettingsUpdate>(ClientPacketId.GameSettingsUpdated,
                OnGameSettingsUpdated);
            
            // Register the Hero Controller Start, which is when the local player spawns
            On.HeroController.Start += (orig, self) => {
                // Execute the original method
                orig(self);
                // If we are connect to a server, add a username to the player object
                if (networkManager.GetNetClient().IsConnected) {
                    _playerManager.AddNameToPlayer(HeroController.instance.gameObject, _username, _playerManager.LocalPlayerTeam);
                }
            };
            networkManager.GetNetClient().RegisterOnConnect(() => {
                // We should only be able to connect during a gameplay scene,
                // which is when the player is spawned already, so we can add the username
                _playerManager.AddNameToPlayer(HeroController.instance.gameObject, _username, _playerManager.LocalPlayerTeam);
            });

            // Register handlers for scene change and player update
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
            On.HeroController.Update += OnPlayerUpdate;

            // Register client connect handler
            _netClient.RegisterOnConnect(OnClientConnect);

            _netClient.RegisterOnHeartBeat(OnHeartBeat);

            // Register application quit handler
            ModHooks.Instance.ApplicationQuitHook += OnApplicationQuit;


            // Prevent changing the timescale if the client is connected to ensure synchronisation between clients
            On.GameManager.SetTimeScale_float += (orig, self, scale) => {
                if (!_netClient.IsConnected) {
                    orig(self, scale);
                } else {
                    // Always put the time scale to 1.0, thus never allowing the game to change speed
                    // This is to prevent desyncs in multiplayer
                    orig(self, 1.0f);
                }
            };
            // Register pause callback to make sure the player doesn't keep dashing or moving
            On.HeroController.Pause += (orig, self) => {
                if (!_netClient.IsConnected) {
                    orig(self);
                    return;
                }

                // We simply call the private ResetInput method to prevent the knight from continuing movement
                // while the game is paused
                typeof(HeroController).InvokeMember(
                    "ResetInput",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                    null,
                    HeroController.instance,
                    null
                );
            };
            
            // To make sure that if we are paused, and we enter a screen transition,
            // we still go through it. So we unpause first, then execute the original method
            On.TransitionPoint.OnTriggerEnter2D += (orig, self, obj) => {
                // Unpause if paused
                if (UIManager.instance != null) {
                    if (UIManager.instance.uiState.Equals(UIState.PAUSED)) {
                        UIManager.instance.TogglePauseGame();
                    }
                }

                // Execute original method
                orig(self, obj);
            };
        }

        /**
         * Connect the client with the server with the given address and port
         * and use the given username
         */
        public void Connect(string address, int port, string username) {
            // Stop existing client
            if (_netClient.IsConnected) {
                Disconnect();
            }

            // Store username, so we know what to send the server if we are connected
            _username = username;

            // Connect the network client
            _netClient.Connect(address, port);
        }

        /**
         * Disconnect the local client from the server
         */
        public void Disconnect(bool sendDisconnect = true) {
            if (_netClient.IsConnected) {
                if (sendDisconnect) {
                    // First send the server that we are disconnecting
                    Logger.Info(this, "Sending PlayerDisconnect packet");
                    _netClient.UpdateManager.SetPlayerDisconnect();
                }

                // Then actually disconnect
                _netClient.Disconnect();

                // Reset the local player's team
                _playerManager.LocalPlayerTeam = Team.None;

                // Reset the local player's skin
                _serverKnightsManager.disconnected();
                // Clear all players
                _playerManager.DestroyAllPlayers();

                // Remove name
                _playerManager.RemoveNameFromLocalPlayer();
                
                // Check whether the game is in the pause menu and reset timescale to 0 in that case
                if (UIManager.instance.uiState.Equals(UIState.PAUSED)) {
                    SetGameManagerTimeScale(0);
                }
                
                UI.UIManager.InfoBox.AddMessage("You are disconnected from the server");
            } else {
                Logger.Warn(this, "Could not disconnect client, it was not connected");
            }
            
            // We are disconnected, so we stopped updating heart beats
            MonoBehaviourUtil.Instance.OnUpdateEvent -= CheckHeartBeat;
            
            _heartBeatReceiveStopwatch.Stop();
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

        /*public void RegisterServerKnightChange(Action ServerKnightChange) {
            ServerKnightChangeEvent += ServerKnightChange;
        }*/

        public void ServerKnightSend(int type,ushort payload){
            if (!_netClient.IsConnected) {
                return;
            }
            //_netClient.UpdateManager.ServerKnightUpdate(type,payload);
        }

        


        public void ChangeTeam(Team team) {
            if (!_netClient.IsConnected) {
                return;
            }

            _playerManager.OnLocalPlayerTeamUpdate(team);

            _netClient.UpdateManager.SetTeamUpdate(team);
            
            UI.UIManager.InfoBox.AddMessage($"You are now in Team {team}");
        }

        private void OnClientConnect() {
            Logger.Info(this, "Client is connected, sending Hello packet");

            // If we are in a non-gameplay scene, we transmit that we are not active yet
            var currentSceneName = SceneUtil.GetCurrentSceneName();
            if (SceneUtil.IsNonGameplayScene(currentSceneName)) {
                Logger.Error(this,
                    $"Client connected during a non-gameplay scene named {currentSceneName}, this should never happen!");
                return;
            }

            var transform = HeroController.instance.transform;

            Logger.Info(this, "Sending Hello packet");

            _netClient.UpdateManager.SetHelloServerData(
                _username,
                SceneUtil.GetCurrentSceneName(),
                transform.position,
                transform.localScale.x > 0,
                (ushort) _animationManager.GetCurrentAnimationClip()
            );
            
            // Since we are probably in the pause menu when we connect, set the timescale so the game
            // is running while paused
            SetGameManagerTimeScale(1.0f);

            // We have established a TCP connection so we should receive heart beats now
            _heartBeatReceiveStopwatch.Reset();
            _heartBeatReceiveStopwatch.Start();
            
            MonoBehaviourUtil.Instance.OnUpdateEvent += CheckHeartBeat;
            
            UI.UIManager.InfoBox.AddMessage("You are connected to the server");
        }

        private void OnServerShutdown() {
            Logger.Info(this, "Server is shutting down, clearing players and disconnecting client");

            // Disconnect without sending the server that we disconnect, because the server is shutting down anyway
            Disconnect(false);
        }
        
        private void OnPlayerConnect(PlayerConnect playerConnect) {
            Logger.Info(this, $"Received PlayerConnect data for ID: {playerConnect.Id}");

            UI.UIManager.InfoBox.AddMessage($"Player '{playerConnect.Username}' connected to the server");
        }
        
        private void OnPlayerDisconnect(ClientPlayerDisconnect playerDisconnect) {
            var id = playerDisconnect.Id;
            var username = playerDisconnect.Username;

            Logger.Info(this, $"Received PlayerDisconnect data for ID: {id}");

            // Destroy player object
            _playerManager.DestroyPlayer(id);

            // Destroy map icon
            _mapManager.RemovePlayerIcon(id);

            UI.UIManager.InfoBox.AddMessage($"Player '{username}' disconnected from the server");
        }
        
        private void OnPlayerAlreadyInScene(ClientPlayerAlreadyInScene alreadyInScene) {
            Logger.Info(this, "Received AlreadyInScene packet");
            
            foreach (var playerEnterScene in alreadyInScene.PlayerEnterSceneList) {
                Logger.Info(this, $"Updating already in scene player with ID: {playerEnterScene.Id}");
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

        private void OnPlayerEnterScene(ClientPlayerEnterScene playerData) {
            // Read ID from player data
            var id = playerData.Id;

            Logger.Info(this, $"Player {id} entered scene, spawning player");

            _playerManager.SpawnPlayer(id, playerData.Username, playerData.Position, playerData.Scale, playerData.Team, playerData.Skin);
            _animationManager.UpdatePlayerAnimation(id, playerData.AnimationClipId, 0);

        }

        private void OnPlayerLeaveScene(GenericClientData data) {
            // Destroy corresponding player
            _playerManager.DestroyPlayer(data.Id);

            Logger.Info(this, $"Player {data.Id} left scene, destroying player");
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
                _entityManager.UpdateEntityPosition(entityUpdate.EntityType, entityUpdate.Id,
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
                    entityUpdate.EntityType,
                    entityUpdate.Id,
                    entityUpdate.State,
                    variables
                );
            }
        }

        private void OnPlayerTeamUpdate(ClientPlayerTeamUpdate playerTeamUpdate) {
            var id = playerTeamUpdate.Id;
            var team = playerTeamUpdate.Team;
            
            Logger.Info(this, $"Received PlayerTeamUpdate data for ID: {id}");

            _playerManager.OnPlayerTeamUpdate(id, team);

            UI.UIManager.InfoBox.AddMessage($"Player '{playerTeamUpdate.Username}' is now in Team {team}");
        }

        private void OnServerKnightUpdate(ClientServerKnightUpdate packet) {
            Logger.Info(this, $"Received ServerKnightUpdate data for ID: {packet.Id}");

            ClientPlayerData player = _playerManager.GetPlayer(packet.Id);
            if(player != null){
                _serverKnightsManager.OnServerKnightUpdate(player,packet.Id, packet.Skin,packet.Emote);
            }
        }

        private void OnGameSettingsUpdated(GameSettingsUpdate update) {
            var pvpChanged = false;
            var bodyDamageChanged = false;
            var displayNamesChanged = false;
            var alwaysShowMapChanged = false;
            var onlyCompassChanged = false;
            var teamsChanged = false;

            // Check whether the PvP state changed
            if (_gameSettings.IsPvpEnabled != update.GameSettings.IsPvpEnabled) {
                pvpChanged = true;

                var message = $"PvP is now {(update.GameSettings.IsPvpEnabled ? "enabled" : "disabled")}";
                
                UI.UIManager.InfoBox.AddMessage(message);
                Logger.Info(this, message);
            }

            // Check whether the body damage state changed
            if (_gameSettings.IsBodyDamageEnabled != update.GameSettings.IsBodyDamageEnabled) {
                bodyDamageChanged = true;

                var message =
                    $"Body damage is now {(update.GameSettings.IsBodyDamageEnabled ? "enabled" : "disabled")}";

                UI.UIManager.InfoBox.AddMessage(message);
                Logger.Info(this, message);
            }

            // Check whether the always show map icons state changed
            if (_gameSettings.AlwaysShowMapIcons != update.GameSettings.AlwaysShowMapIcons) {
                alwaysShowMapChanged = true;

                var message =
                    $"Map icons are now{(update.GameSettings.AlwaysShowMapIcons ? "" : " not")} always visible";

                UI.UIManager.InfoBox.AddMessage(message);
                Logger.Info(this, message);
            }

            // Check whether the wayward compass broadcast state changed
            if (_gameSettings.OnlyBroadcastMapIconWithWaywardCompass !=
                update.GameSettings.OnlyBroadcastMapIconWithWaywardCompass) {
                onlyCompassChanged = true;

                var message =
                    $"Map icons are {(update.GameSettings.OnlyBroadcastMapIconWithWaywardCompass ? "now only" : "not")} broadcast when wearing the Wayward Compass charm";

                UI.UIManager.InfoBox.AddMessage(message);
                Logger.Info(this, message);
            }
            
            // Check whether the display names setting changed
            if (_gameSettings.DisplayNames != update.GameSettings.DisplayNames) {
                displayNamesChanged = true;

                var message = $"Names are {(update.GameSettings.DisplayNames ? "now" : "no longer")} displayed";
                
                UI.UIManager.InfoBox.AddMessage(message);
                Logger.Info(this, message);
            }
            
            // Check whether the teams enabled setting changed
            if (_gameSettings.TeamsEnabled != update.GameSettings.TeamsEnabled) {
                teamsChanged = true;

                var message = $"Team are {(update.GameSettings.TeamsEnabled ? "now" : "no longer")} enabled";

                UI.UIManager.InfoBox.AddMessage(message);
                Logger.Info(this, message);
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
        }

        private void OnSceneChange(Scene oldScene, Scene newScene) {
            Logger.Info(this, $"Scene changed from {oldScene.name} to {newScene.name}");

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
            
            _serverKnightsManager.OnSceneChange();

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
                    var position = Vector3.zero;
                    var scale = Vector3.zero;
                    ushort animationClipId = 0;

                    // If we do have a HeroController instance, use its values
                    if (HeroController.instance != null) {
                        var transform = HeroController.instance.transform;
                        
                        position = transform.position;
                        scale = transform.localScale;
                        animationClipId = (ushort) _animationManager.GetCurrentAnimationClip();
                    }
            
                    Logger.Info(this, "Sending EnterScene packet");

                    _netClient.UpdateManager.SetEnterSceneData(
                        SceneUtil.GetCurrentSceneName(),
                        position,
                        scale.x > 0,
                        _serverKnightsManager.skinManager.LocalPlayerSkin,
                        animationClipId
                    );
                } else {
                    // If this was not the first position update after a scene change,
                    // we can simply send a position update packet
                    _netClient.UpdateManager.UpdatePlayerPosition(newPosition);
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

        private void OnHeartBeat() {
            // We received an update from the server, so we can reset the heart beat stopwatch
            _heartBeatReceiveStopwatch.Reset();
            // Only start the stopwatch again if we are actually connected
            if (_netClient.IsConnected) {
                _heartBeatReceiveStopwatch.Start();
            }
        }

        private void CheckHeartBeat() {
            if (!_netClient.IsConnected) {
                return;
            }
            
            // If we have not received a heart beat recently
            if (_heartBeatReceiveStopwatch.ElapsedMilliseconds > ConnectionTimeout) {
                Logger.Info(this, 
                    $"We didn't receive a heart beat from the server in {ConnectionTimeout} milliseconds, disconnecting ({_heartBeatReceiveStopwatch.ElapsedMilliseconds})");
                
                Disconnect();
            }
        }

        private void OnApplicationQuit() {
            if (!_netClient.IsConnected) {
                return;
            }

            // Send a disconnect packet before exiting the application
            Logger.Info(this, "Sending PlayerDisconnect packet");
            _netClient.UpdateManager.SetDisconnect();
            _netClient.Disconnect();
        }

        private static void SetGameManagerTimeScale(float timeScale) {
            typeof(global::GameManager).InvokeMember(
                "SetTimeScale", 
                BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                Type.DefaultBinder,
                global::GameManager.instance, 
                new object[] {timeScale}
            );
        }
    }
}