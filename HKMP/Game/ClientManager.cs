using HKMP.Animation;
using HKMP.Networking;
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
        private readonly NetworkManager _networkManager;
        private readonly UI.UIManager _uiManager;
        private readonly PlayerManager _playerManager;
        private readonly AnimationManager _animationManager;

        // Keeps track of the last updated location of the local player object
        private Vector3 _lastPosition;

        // Keeps track of the last updated scale of the local player object
        private Vector3 _lastScale;

        public ClientManager(NetworkManager networkManager, UI.UIManager uiManager, PlayerManager playerManager,
            AnimationManager animationManager, PacketManager packetManager) {
            _networkManager = networkManager;
            _uiManager = uiManager;
            _playerManager = playerManager;
            _animationManager = animationManager;

            // Register packet handlers
            packetManager.RegisterClientPacketHandler<ServerShutdownPacket>(PacketId.ServerShutdown, OnServerShutdown);
            packetManager.RegisterClientPacketHandler<PlayerEnterScenePacket>(PacketId.PlayerEnterScene, OnPlayerEnterScene);
            packetManager.RegisterClientPacketHandler<PlayerLeaveScenePacket>(PacketId.PlayerLeaveScene, OnPlayerLeaveScene);
            packetManager.RegisterClientPacketHandler<ClientPlayerPositionUpdatePacket>(PacketId.ClientPlayerPositionUpdate, OnPlayerPositionUpdate);
            packetManager.RegisterClientPacketHandler<ClientPlayerScaleUpdatePacket>(PacketId.ClientPlayerScaleUpdate, OnPlayerScaleUpdate);

            // Register handlers for scene change and player update
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
            On.HeroController.Update += OnPlayerUpdate;

            // Register client connect handler
            _networkManager.RegisterOnConnect(OnClientConnect);

            // Register application quit handler
            ModHooks.Instance.ApplicationQuitHook += OnApplicationQuit;
        }

        private void OnClientConnect() {
            Logger.Info(this, "Client is connected, sending Hello packet");

            // If we are in a non-gameplay scene, we transmit that we are not active yet
            var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (SceneUtil.IsNonGameplayScene(currentSceneName)) {
                Logger.Error(this, $"Client connected during a non-gameplay scene named {currentSceneName}, this should never happen!");
                return;
            }

            var transform = HeroController.instance.transform;

            // Fill the hello packet with necessary data
            var helloPacket = new HelloServerPacket {
                Username = _uiManager.GetEnteredUsername(),
                SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                Position = transform.position,
                Scale = transform.localScale,
                AnimationClipName = HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name
            };
            helloPacket.CreatePacket();

            _networkManager.GetNetClient().SendTcp(helloPacket);
        }

        private void OnServerShutdown(ServerShutdownPacket packet) {
            Logger.Info(this, "Server is shutting down, clearing players and disconnecting client");

            // Clear all players
            _playerManager.DestroyAllPlayers();

            // Disconnect our client
            _networkManager.DisconnectClient();

            // Reset the UI
            _uiManager.OnClientDisconnect();
        }

        private void OnPlayerEnterScene(PlayerEnterScenePacket packet) {
            // Read ID from packet
            var id = packet.Id;

            Logger.Info(this, $"Player {id} entered scene, spawning player");

            _playerManager.SpawnPlayer(id, packet.Username);
            _playerManager.UpdatePosition(id, packet.Position);
            _playerManager.UpdateScale(id, packet.Scale);
            _animationManager.UpdatePlayerAnimation(id, packet.AnimationClipName);
        }

        private void OnPlayerLeaveScene(PlayerLeaveScenePacket packet) {
            // Destroy corresponding player
            _playerManager.DestroyPlayer(packet.Id);

            Logger.Info(this, $"Player {packet.Id} left scene, destroying player");
        }

        private void OnPlayerPositionUpdate(ClientPlayerPositionUpdatePacket packet) {
            // Update the position of the player object corresponding to this ID
            _playerManager.UpdatePosition(packet.Id, packet.Position);
        }

        private void OnPlayerScaleUpdate(ClientPlayerScaleUpdatePacket packet) {
            // Update the position of the player object corresponding to this ID
            _playerManager.UpdateScale(packet.Id, packet.Scale);
        }

        private void OnSceneChange(Scene oldScene, Scene newScene) {
            Logger.Info(this, $"Scene changed from {oldScene.name} to {newScene.name}");

            // Always destroy existing players, because we changed scenes
            _playerManager.DestroyAllPlayers();

            // Ignore scene changes to non-gameplay scenes
            if (SceneUtil.IsNonGameplayScene(newScene.name)) {
                return;
            }

            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }

            var transform = HeroController.instance.transform;
            
            // Create the SceneChange packet
            var packet = new PlayerChangeScenePacket {
                NewSceneName = newScene.name,
                Position = transform.position,
                Scale = transform.localScale,
                AnimationClipName = HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name
            };
            packet.CreatePacket();

            // Send it to the server
            _networkManager.GetNetClient().SendTcp(packet);
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
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }

            var newPosition = HeroController.instance.transform.position;
            // If the position changed since last check
            if (newPosition != _lastPosition) {
                // Create player position packet
                var positionUpdatePacket = new ServerPlayerPositionUpdatePacket {
                    Position = newPosition
                };
                positionUpdatePacket.CreatePacket();
                
                // Send packet over UDP
                _networkManager.GetNetClient().SendUdp(positionUpdatePacket);

                // Update the last position, since it changed
                _lastPosition = newPosition;
            }

            var newScale = HeroController.instance.transform.localScale;
            // If the scale changed since last check
            if (newScale != _lastScale) {
                // Create player scale packet
                var scaleUpdatePacket = new ServerPlayerScaleUpdatePacket {
                    Scale = newScale
                };
                scaleUpdatePacket.CreatePacket();
                
                // Send packet over UDP
                _networkManager.GetNetClient().SendUdp(scaleUpdatePacket);

                // Update the last scale, since it changed
                _lastScale = newScale;
            }
        }

        private void OnApplicationQuit() {
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }

            // Send a disconnect packet before exiting the application
            var disconnectPacket = new PlayerDisconnectPacket();
            disconnectPacket.CreatePacket();
            
            _networkManager.GetNetClient().SendTcp(disconnectPacket);
            _networkManager.GetNetClient().Disconnect();
        }
    }
}