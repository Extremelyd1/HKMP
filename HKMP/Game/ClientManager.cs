using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HKMP.Game {
    // TODO: spawning a player gives them offscreen position, we need to immediately update their position
    /**
     * Class that manages the client state (similar to ServerManager).
     * For example keeping track of spawning/destroying player objects.
     */
    public class ClientManager {
        private readonly NetworkManager _networkManager;
        private readonly UI.UIManager _uiManager;
        
        private readonly PlayerManager _playerManager;

        private Vector3 _lastPosition;

        public ClientManager(NetworkManager networkManager, PacketManager packetManager, UI.UIManager uiManager) {
            _networkManager = networkManager;
            _uiManager = uiManager;

            _playerManager = new PlayerManager();
            
            // Register packet handler
            packetManager.RegisterClientPacketHandler(PacketId.Shutdown, OnServerShutdown);
            packetManager.RegisterClientPacketHandler(PacketId.PlayerEnterScene, OnPlayerEnterScene);
            packetManager.RegisterClientPacketHandler(PacketId.PlayerLeaveScene, OnPlayerLeaveScene);
            packetManager.RegisterClientPacketHandler(PacketId.PlayerPositionUpdate, OnPlayerPositionUpdate);
            
            // Register handlers for scene change and player update
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
            On.HeroController.Update += OnPlayerUpdate;
            
            // Register client connect handler
            _networkManager.RegisterOnConnect(OnClientConnect);
        }

        private void OnClientConnect() {
            Logger.Info(this, "Client is connected, sending Hello packet");

            var helloPacket = new Packet(PacketId.HelloServer);

            // If we are in a non-gameplay scene, we transmit empty values for scene and position
            var currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (SceneUtil.IsNonGameplayScene(currentSceneName)) {
                helloPacket.Write("NonGameplay");
                helloPacket.Write(Vector3.zero);
            } else {
                helloPacket.Write(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                helloPacket.Write(HeroController.instance.transform.position);
            }

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
            var position = packet.ReadVector3();
            
            Logger.Info(this, $"Player {id} entered scene, spawning player");
            
            _playerManager.SpawnPlayer(id);
            _playerManager.UpdatePosition(id, position);
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
                var playerUpdatePacket = new Packet(PacketId.PlayerPositionUpdate);
                playerUpdatePacket.Write(newPosition);

                // Send packet over UDP
                _networkManager.GetNetClient().SendUdp(playerUpdatePacket);
                
                // Update the last position, since it changed
                _lastPosition = newPosition;
            }
        }

    }
}