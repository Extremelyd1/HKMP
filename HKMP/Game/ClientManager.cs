using System;
using System.Diagnostics;
using System.Reflection;
using GlobalEnums;
using HKMP.Animation;
using HKMP.Networking;
using HKMP.Networking.Client;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HKMP.Game {
    /**
     * Class that manages the client state (similar to ServerManager).
     * For example keeping track of spawning/destroying player objects.
     */
    public class ClientManager {
        // How long to wait before disconnecting from the server after not receiving a heart beat
        private const int ConnectionTimeout = 5000;
        
        private readonly NetClient _netClient;
        private readonly PlayerManager _playerManager;
        private readonly AnimationManager _animationManager;
        private readonly MapManager _mapManager;
        private readonly Settings.GameSettings _gameSettings;

        // The username that was used to connect with
        private string _username;

        // Keeps track of the last updated location of the local player object
        private Vector3 _lastPosition;

        // Keeps track of the last updated scale of the local player object
        private Vector3 _lastScale;

        // Stopwatch to keep track of the time since the last server packet
        private readonly Stopwatch _heartBeatReceiveStopwatch;

        public ClientManager(NetworkManager networkManager, PlayerManager playerManager,
            AnimationManager animationManager, MapManager mapManager, Settings.GameSettings gameSettings,
            PacketManager packetManager) {
            _netClient = networkManager.GetNetClient();
            _playerManager = playerManager;
            _animationManager = animationManager;
            _mapManager = mapManager;
            _gameSettings = gameSettings;
            
            _heartBeatReceiveStopwatch = new Stopwatch();

            // Register packet handlers
            packetManager.RegisterClientPacketHandler<ServerShutdownPacket>(PacketId.ServerShutdown, OnServerShutdown);
            packetManager.RegisterClientPacketHandler<ClientPlayerDisconnectPacket>(PacketId.PlayerDisconnect, OnPlayerDisconnect);
            packetManager.RegisterClientPacketHandler<PlayerEnterScenePacket>(PacketId.PlayerEnterScene,
                OnPlayerEnterScene);
            packetManager.RegisterClientPacketHandler<PlayerLeaveScenePacket>(PacketId.PlayerLeaveScene,
                OnPlayerLeaveScene);
            packetManager.RegisterClientPacketHandler<ClientPlayerUpdatePacket>(
                PacketId.PlayerUpdate, OnPlayerUpdate);
            packetManager.RegisterClientPacketHandler<GameSettingsUpdatePacket>(PacketId.GameSettingsUpdated,
                OnGameSettingsUpdated);

            // Register handlers for scene change and player update
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
            On.HeroController.Update += OnPlayerUpdate;

            // Register client connect handler
            _netClient.RegisterOnConnect(OnClientConnect);

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
                    _netClient.SendTcp(new ServerPlayerDisconnectPacket().CreatePacket());
                }

                // Then actually disconnect
                _netClient.Disconnect();

                // Clear all players
                _playerManager.DestroyAllPlayers();

                // Remove name
                _playerManager.RemoveNameFromLocalPlayer();
                
                // Check whether the game is in the pause menu and reset timescale to 0 in that case
                if (UIManager.instance.uiState.Equals(UIState.PAUSED)) {
                    SetGameManagerTimeScale(0);
                }
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

        private void OnClientConnect() {
            Logger.Info(this, "Client is connected, sending Hello packet");

            // If we are in a non-gameplay scene, we transmit that we are not active yet
            var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (SceneUtil.IsNonGameplayScene(currentSceneName)) {
                Logger.Error(this,
                    $"Client connected during a non-gameplay scene named {currentSceneName}, this should never happen!");
                return;
            }

            var transform = HeroController.instance.transform;

            // Fill the hello packet with necessary data
            var helloPacket = new HelloServerPacket {
                Username = _username,
                SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                Position = transform.position,
                Scale = transform.localScale,
                AnimationClipName = HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name
            };
            helloPacket.CreatePacket();
            
            Logger.Info(this, "Sending Hello packet");

            _netClient.SendTcp(helloPacket);
            
            // Since we are probably in the pause menu when we connect, set the timescale so the game
            // is running while paused
            SetGameManagerTimeScale(1.0f);

            // We have established a TCP connection so we should receive heart beats now
            _heartBeatReceiveStopwatch.Reset();
            _heartBeatReceiveStopwatch.Start();
            
            MonoBehaviourUtil.Instance.OnUpdateEvent += CheckHeartBeat;
        }

        private void OnServerShutdown(ServerShutdownPacket packet) {
            Logger.Info(this, "Server is shutting down, clearing players and disconnecting client");

            // Disconnect without sending the server that we disconnect, because the server is shutting down anyway
            Disconnect(false);
        }
        
        private void OnPlayerDisconnect(ClientPlayerDisconnectPacket packet) {
            Logger.Info(this, $"Received PlayerDisconnect packet for ID: {packet.Id}");
        
            // Destroy player object
            _playerManager.DestroyPlayer(packet.Id);
            
            // Destroy map icon
            _mapManager.RemovePlayerIcon(packet.Id);
        }

        private void OnPlayerEnterScene(PlayerEnterScenePacket packet) {
            // Read ID from packet
            var id = packet.Id;

            Logger.Info(this, $"Player {id} entered scene, spawning player");

            _playerManager.SpawnPlayer(id, packet.Username);
            _playerManager.UpdatePosition(id, packet.Position);
            _playerManager.UpdateScale(id, packet.Scale);
            _animationManager.UpdatePlayerAnimation(id, packet.AnimationClipName, 0);
        }

        private void OnPlayerLeaveScene(PlayerLeaveScenePacket packet) {
            // Destroy corresponding player
            _playerManager.DestroyPlayer(packet.Id);

            Logger.Info(this, $"Player {packet.Id} left scene, destroying player");
        }

        private void OnPlayerUpdate(ClientPlayerUpdatePacket packet) {
            // We received an update from the server, so we can reset the heart beat stopwatch
            _heartBeatReceiveStopwatch.Reset();
            // Only start the stopwatch again if we are actually connected
            if (_netClient.IsConnected) {
                _heartBeatReceiveStopwatch.Start();
            }
            
            // Update the positions of the player objects in the packet
            foreach (var playerUpdate in packet.PlayerUpdates) {
                if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Position)) {
                    _playerManager.UpdatePosition(playerUpdate.Id, playerUpdate.Position);
                }

                if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Scale)) {
                    _playerManager.UpdateScale(playerUpdate.Id, playerUpdate.Scale);
                }

                if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.MapPosition)) {
                    _mapManager.OnPlayerMapUpdate(playerUpdate.Id, playerUpdate.MapPosition);                    
                }

                if (playerUpdate.UpdateTypes.Contains(UpdatePacketType.Animation)) {
                    foreach (var animationInfo in playerUpdate.AnimationInfos) {
                        _animationManager.OnPlayerAnimationUpdate(
                            playerUpdate.Id,
                            animationInfo.ClipName,
                            animationInfo.Frame,
                            animationInfo.EffectInfo
                        );
                    }
                }
            }
        }

        private void OnGameSettingsUpdated(GameSettingsUpdatePacket packet) {
            var pvpChanged = false;
            var bodyDamageChanged = false;
            var displayNamesChanged = false;

            // Check whether the PvP state changed
            if (_gameSettings.IsPvpEnabled != packet.GameSettings.IsPvpEnabled) {
                pvpChanged = true;

                Logger.Info(this, $"PvP is now {(packet.GameSettings.IsPvpEnabled ? "Enabled" : "Disabled")}");
            }

            // Check whether the body damage state changed
            if (_gameSettings.IsBodyDamageEnabled != packet.GameSettings.IsBodyDamageEnabled) {
                bodyDamageChanged = true;

                Logger.Info(this,
                    $"Body damage is now {(packet.GameSettings.IsBodyDamageEnabled ? "Enabled" : "Disabled")}");
            }

            // Check whether the always show map icons state changed
            if (_gameSettings.AlwaysShowMapIcons != packet.GameSettings.AlwaysShowMapIcons) {
                Logger.Info(this,
                    $"Map icons are {(packet.GameSettings.AlwaysShowMapIcons ? "now" : "not")} always visible");
            }

            // Check whether the wayward compass broadcast state changed
            if (_gameSettings.OnlyBroadcastMapIconWithWaywardCompass !=
                packet.GameSettings.OnlyBroadcastMapIconWithWaywardCompass) {
                Logger.Info(this,
                    $"Map icons are {(packet.GameSettings.OnlyBroadcastMapIconWithWaywardCompass ? "now" : "not")} only broadcast when wearing the Wayward Compass charm");
            }
            
            // Check whether the display names setting changed
            if (_gameSettings.DisplayNames != packet.GameSettings.DisplayNames) {
                displayNamesChanged = true;
                
                Logger.Info(this, $"Names are {(packet.GameSettings.DisplayNames ? "now" : "no longer")} displayed");
            }

            // Update the settings so callbacks can read updated values
            _gameSettings.SetAllProperties(packet.GameSettings);

            // Only update the player manager if the either PvP or body damage have been changed
            if (pvpChanged || bodyDamageChanged || displayNamesChanged) {
                _playerManager.OnGameSettingsUpdated(pvpChanged || bodyDamageChanged, displayNamesChanged);
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

            // Ignore scene changes from and to non-gameplay scenes
            if (SceneUtil.IsNonGameplayScene(oldScene.name) && SceneUtil.IsNonGameplayScene(newScene.name)) {
                return;
            }
            
            // Set some default values for the packet variables in case we don't have a HeroController instance
            // This might happen when we are in a non-gameplay scene without the knight
            var position = Vector3.zero;
            var scale = Vector3.zero;
            var animationClipName = "";

            // If we do have a HeroController instance, use its values
            if (HeroController.instance != null) {
                var transform = HeroController.instance.transform;
                position = transform.position;
                scale = transform.localScale;
                animationClipName = HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name;
            }

            // Create the SceneChange packet
            var packet = new PlayerChangeScenePacket {
                NewSceneName = newScene.name,
                Position = position,
                Scale = scale,
                AnimationClipName = animationClipName
            };
            packet.CreatePacket();
            
            Logger.Info(this, "Sending PlayerChangeScene packet");

            // Send it to the server
            _netClient.SendTcp(packet);
        }

        private void OnPlayerUpdate(On.HeroController.orig_Update orig, HeroController self) {
            // Make sure the original method executes
            orig(self);

            // Ignore player position updates on non-gameplay scenes
            var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
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
                _netClient.SendPositionUpdate(newPosition);

                // Update the last position, since it changed
                _lastPosition = newPosition;
            }
            
            var newScale = heroTransform.localScale;
            // If the scale changed since last check
            if (newScale != _lastScale) {
                _netClient.SendScaleUpdate(newScale);
                
                // Update the last scale, since it changed
                _lastScale = newScale;
            }
        }

        private void CheckHeartBeat() {
            if (!_netClient.IsConnected) {
                return;
            }
            
            // If we have not received a heart beat recently
            if (_heartBeatReceiveStopwatch.ElapsedMilliseconds > ConnectionTimeout) {
                // Logger.Info(this, 
                //     $"We didn't receive a heart beat from the server in {ConnectionTimeout} milliseconds, disconnecting ({_heartBeatReceiveStopwatch.ElapsedMilliseconds})");
                //
                // Disconnect();
            }
        }

        private void OnApplicationQuit() {
            if (!_netClient.IsConnected) {
                return;
            }

            // Send a disconnect packet before exiting the application
            Logger.Info(this, "Sending PlayerDisconnect packet");
            _netClient.SendTcp(new ServerPlayerDisconnectPacket().CreatePacket());
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