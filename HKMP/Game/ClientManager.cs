using HKMP.Animation;
using HKMP.Networking;
using HKMP.Networking.Packet;
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
            packetManager.RegisterClientPacketHandler(PacketId.Shutdown, OnServerShutdown);
            packetManager.RegisterClientPacketHandler(PacketId.PlayerEnterScene, OnPlayerEnterScene);
            packetManager.RegisterClientPacketHandler(PacketId.PlayerLeaveScene, OnPlayerLeaveScene);
            packetManager.RegisterClientPacketHandler(PacketId.PlayerPositionUpdate, OnPlayerPositionUpdate);
            packetManager.RegisterClientPacketHandler(PacketId.PlayerScaleUpdate, OnPlayerScaleUpdate);

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

            var helloPacket = new Packet(PacketId.HelloServer);

            // If we are in a non-gameplay scene, we transmit that we are not active yet
            var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (SceneUtil.IsNonGameplayScene(currentSceneName)) {
                Logger.Error(this, $"Client connected during a non-gameplay scene named {currentSceneName}, this should never happen!");
                return;
            }
            
            // Fill the hello packet with necessary data
            helloPacket.Write(_uiManager.GetEnteredUsername());
            helloPacket.Write(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            helloPacket.Write(HeroController.instance.transform.position);
            helloPacket.Write(HeroController.instance.transform.localScale);
            helloPacket.Write(HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name);

            _networkManager.GetNetClient().SendTcp(helloPacket);
        }

        private void OnServerShutdown(Packet packet) {
            Logger.Info(this, "Server is shutting down, clearing players and disconnecting client");

            // Clear all players
            _playerManager.DestroyAllPlayers();

            // Disconnect our client
            _networkManager.DisconnectClient();

            // Reset the UI
            _uiManager.OnClientDisconnect();
        }

        private void OnPlayerEnterScene(Packet packet) {
            // Read ID from packet and spawn player
            var id = packet.ReadInt();
            var username = packet.ReadString();
            var position = packet.ReadVector3();
            var scale = packet.ReadVector3();
            var clipName = packet.ReadString();

            Logger.Info(this, $"Player {id} entered scene, spawning player");

            _playerManager.SpawnPlayer(id, username);
            _playerManager.UpdatePosition(id, position);
            _playerManager.UpdateScale(id, scale);
            _animationManager.UpdatePlayerAnimation(id, clipName);
        }

        private void OnPlayerLeaveScene(Packet packet) {
            // Read ID from packet and destroy player
            var id = packet.ReadInt();
            _playerManager.DestroyPlayer(id);

            Logger.Info(this, $"Player {id} left scene, destroying player");
        }

        private void OnPlayerPositionUpdate(Packet packet) {
            // Read ID and new position from packet
            var id = packet.ReadInt();
            var position = packet.ReadVector3();
            // Update the position of the player object corresponding to this ID
            _playerManager.UpdatePosition(id, position);
        }

        private void OnPlayerScaleUpdate(Packet packet) {
            // Read ID and new scale from packet
            var id = packet.ReadInt();
            var scale = packet.ReadVector3();
            // Update the position of the player object corresponding to this ID
            _playerManager.UpdateScale(id, scale);
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

            // Create the SceneChange packet
            var packet = new Packet(PacketId.SceneChange);
            packet.Write(newScene.name);
            packet.Write(HeroController.instance.transform.position);
            packet.Write(HeroController.instance.transform.localScale);
            packet.Write(HeroController.instance.GetComponent<tk2dSpriteAnimator>().CurrentClip.name);

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
                var positionUpdatePacket = new Packet(PacketId.PlayerPositionUpdate);
                positionUpdatePacket.Write(newPosition);

                // Send packet over UDP
                _networkManager.GetNetClient().SendUdp(positionUpdatePacket);

                // Update the last position, since it changed
                _lastPosition = newPosition;
            }

            var newScale = HeroController.instance.transform.localScale;
            // If the scale changed since last check
            if (newScale != _lastScale) {
                // Create player scale packet
                var scaleUpdatePacket = new Packet(PacketId.PlayerScaleUpdate);
                scaleUpdatePacket.Write(newScale);

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
            var disconnectPacket = new Packet(PacketId.Disconnect);
            _networkManager.GetNetClient().SendTcp(disconnectPacket);
            _networkManager.GetNetClient().Disconnect();
        }
    }
}