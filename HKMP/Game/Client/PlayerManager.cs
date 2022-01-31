using System;
using System.Collections.Generic;
using Hkmp.Fsm;
using Hkmp.Game.Client.Skin;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hkmp.Game.Client {
    /**
     * Class that manages player objects, spawning and destroying thereof.
     */
    public class PlayerManager {
        private readonly Game.Settings.GameSettings _gameSettings;
        private readonly SkinManager _skinManager;

        private readonly Dictionary<ushort, ClientPlayerData> _playerData;

        // The team that our local player is on
        public Team LocalPlayerTeam { get; set; } = Team.None;

        private readonly GameObject _playerPrefab;

        public PlayerManager(PacketManager packetManager, Game.Settings.GameSettings gameSettings) {
            _gameSettings = gameSettings;

            _skinManager = new SkinManager();

            _playerData = new Dictionary<ushort, ClientPlayerData>();

            // Create the player prefab, used to instantiate player objects
            _playerPrefab = new GameObject(
                "PlayerPrefab",
                typeof(BoxCollider2D),
                typeof(DamageHero),
                typeof(EnemyHitEffectsUninfected),
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(NonBouncer),
                typeof(SpriteFlash),
                typeof(tk2dSprite),
                typeof(tk2dSpriteAnimator),
                typeof(CoroutineCancelComponent)
            ) {
                layer = 9
            };

            // Add some extra gameObjects related to animation effects
            new GameObject("Attacks") {layer = 9}.transform.SetParent(_playerPrefab.transform);
            new GameObject("Effects") {layer = 9}.transform.SetParent(_playerPrefab.transform);
            new GameObject("Spells") {layer = 9}.transform.SetParent(_playerPrefab.transform);

            _playerPrefab.SetActive(false);
            Object.DontDestroyOnLoad(_playerPrefab);

            // Register packet handlers
            packetManager.RegisterClientPacketHandler<ClientPlayerTeamUpdate>(ClientPacketId.PlayerTeamUpdate,
                OnPlayerTeamUpdate);
            packetManager.RegisterClientPacketHandler<ClientPlayerSkinUpdate>(ClientPacketId.PlayerSkinUpdate,
                OnPlayerSkinUpdate);
        }

        public void UpdatePosition(ushort id, Math.Vector2 position) {
            if (!_playerData.ContainsKey(id)) {
                // Logger.Get().Warn(this, $"Tried to update position for ID {id} while player data did not exists");
                return;
            }

            var playerContainer = _playerData[id].PlayerContainer;
            if (playerContainer != null) {
                var unityPosition = new Vector3(position.X, position.Y);

                playerContainer.GetComponent<PositionInterpolation>().SetNewPosition(unityPosition);
            }
        }

        public void UpdateScale(ushort id, bool scale) {
            if (!_playerData.ContainsKey(id)) {
                // Logger.Get().Warn(this, $"Tried to update scale for ID {id} while player data did not exists");
                return;
            }

            var playerObject = _playerData[id].PlayerObject;
            SetPlayerObjectBoolScale(playerObject, scale);
        }

        private void SetPlayerObjectBoolScale(GameObject playerObject, bool scale) {
            if (playerObject == null) {
            }

            var transform = playerObject.transform;
            var localScale = transform.localScale;
            var currentScaleX = localScale.x;

            if (currentScaleX > 0 != scale) {
                transform.localScale = new Vector3(
                    currentScaleX * -1,
                    localScale.y,
                    localScale.z
                );
            }
        }

        public GameObject GetPlayerObject(ushort id) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Get().Error(this, $"Tried to get the player data that does not exists for ID {id}");
                return null;
            }

            return _playerData[id].PlayerObject;
        }

        public GameObject GetPlayerContainer(ushort id) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Get().Error(this, $"Tried to get the player data that does not exists for ID {id}");
                return null;
            }

            return _playerData[id].PlayerContainer;
        }

        /**
         * Called when the client disconnects from the server.
         * Will reset all player related things to their default values.
         */
        public void OnDisconnect() {
            // Reset the local player's team
            LocalPlayerTeam = Team.None;

            // Clear all players
            DestroyAllPlayers();

            // Remove name
            RemoveNameFromLocalPlayer();

            // Reset the skin of the local player
            _skinManager.ResetLocalPlayerSkin();
        }

        // TODO: investigate whether it is better to disable/setActive(false) player objects instead of destroying
        // and only destroy when player left server
        public void DestroyPlayer(ushort id) {
            if (!_playerData.ContainsKey(id)) {
                Logger.Get().Warn(this, $"Tried to destroy player that does not exists for ID {id}");
                return;
            }

            // Destroy gameObject and remove from mapping
            Object.Destroy(_playerData[id].PlayerContainer);
            _playerData.Remove(id);
        }

        public void DestroyAllPlayers() {
            foreach (var playerData in _playerData.Values) {
                // Destroy gameObject
                Object.Destroy(playerData.PlayerContainer);
            }

            // Clear mapping
            _playerData.Clear();
        }

        public void SpawnPlayer(
            ushort id,
            string name,
            Math.Vector2 position,
            bool scale,
            Team team,
            byte skinId
        ) {
            if (_playerData.ContainsKey(id)) {
                Logger.Get().Warn(this, $"We already have created a player object for ID {id}");
                return;
            }

            // Create a player container
            var playerContainer = new GameObject($"Player Container {id}");
            playerContainer.transform.position = new Vector3(position.X, position.Y);

            playerContainer.AddComponent<PositionInterpolation>();

            // Instantiate the player object from the prefab in the player container
            var playerObject = Object.Instantiate(
                _playerPrefab,
                playerContainer.transform
            );
            Object.DontDestroyOnLoad(playerObject);

            SetPlayerObjectBoolScale(playerObject, scale);

            // Set object and children to active
            playerObject.SetActive(true);
            playerObject.SetActiveChildren(true);

            // Now we need to copy over a lot of variables from the local player object
            var localPlayerObject = HeroController.instance.gameObject;

            // Obtain colliders from both objects
            var collider = playerObject.GetComponent<BoxCollider2D>();
            var localCollider = localPlayerObject.GetComponent<BoxCollider2D>();

            // Copy collider offset and size
            collider.isTrigger = true;
            collider.offset = localCollider.offset;
            collider.size = localCollider.size;
            collider.enabled = true;

            // Copy collider bounds
            var bounds = collider.bounds;
            var localBounds = localCollider.bounds;
            bounds.min = localBounds.min;
            bounds.max = localBounds.max;

            // Disable DamageHero component unless pvp is enabled
            if (_gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled) {
                playerObject.layer = 11;
                playerObject.GetComponent<DamageHero>().enabled = true;
            } else {
                playerObject.layer = 9;
                playerObject.GetComponent<DamageHero>().enabled = false;
            }

            // Copy over mesh filter variables
            var meshFilter = playerObject.GetComponent<MeshFilter>();
            var mesh = meshFilter.mesh;
            var localMesh = localPlayerObject.GetComponent<MeshFilter>().sharedMesh;

            mesh.vertices = localMesh.vertices;
            mesh.normals = localMesh.normals;
            mesh.uv = localMesh.uv;
            mesh.triangles = localMesh.triangles;
            mesh.tangents = localMesh.tangents;

            // Copy mesh renderer material
            var meshRenderer = playerObject.GetComponent<MeshRenderer>();
            meshRenderer.material = new Material(localPlayerObject.GetComponent<MeshRenderer>().material);

            // Disable non bouncer component
            var nonBouncer = playerObject.GetComponent<NonBouncer>();
            nonBouncer.active = false;

            // Copy over animation library
            var spriteAnimator = playerObject.GetComponent<tk2dSpriteAnimator>();
            // Make a smart copy of the sprite animator library so we can
            // modify the animator without having to worry about other player objects
            spriteAnimator.Library = CopyUtil.SmartCopySpriteAnimation(
                localPlayerObject.GetComponent<tk2dSpriteAnimator>().Library,
                playerObject
            );

            AddNameToPlayer(playerContainer, name, team);

            // Let the SkinManager update the skin
            _skinManager.UpdatePlayerSkin(playerObject, skinId);

            // Store the player data in the mapping
            _playerData[id] = new ClientPlayerData(
                playerContainer,
                playerObject,
                team
            );

            // Set whether this player should have body damage
            // Only if:
            // PvP is enabled and body damage is enabled AND
            // (the teams are not equal or if either doesn't have a team)
            ToggleBodyDamage(
                _playerData[id],
                _gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled &&
                (team != LocalPlayerTeam
                 || team.Equals(Team.None)
                 || LocalPlayerTeam.Equals(Team.None))
            );
        }

        public void AddNameToPlayer(GameObject playerContainer, string name, Team team = Team.None) {
            // Create a name object to set the username to, slightly above the player object
            var nameObject = Object.Instantiate(
                new GameObject("Username"),
                playerContainer.transform.position + new Vector3(0, 1.25f, 0),
                Quaternion.identity
            );
            nameObject.name = "Username";
            nameObject.transform.SetParent(playerContainer.transform);
            nameObject.transform.localScale = new Vector3(0.25f, 0.25f, nameObject.transform.localScale.z);
            nameObject.AddComponent<KeepWorldScalePositive>();

            // Add a TextMeshPro component to it, so we can render text
            var textMeshObject = nameObject.AddComponent<TextMeshPro>();
            textMeshObject.text = name.ToUpper();
            textMeshObject.alignment = TextAlignmentOptions.Center;
            textMeshObject.font = FontManager.InGameNameFont;
            textMeshObject.fontSize = 22;
            textMeshObject.outlineWidth = 0.2f;
            textMeshObject.outlineColor = Color.black;

            ChangeNameColor(textMeshObject, team);

            nameObject.SetActive(_gameSettings.DisplayNames);
        }

        private void OnPlayerTeamUpdate(ClientPlayerTeamUpdate playerTeamUpdate) {
            var id = playerTeamUpdate.Id;
            var team = playerTeamUpdate.Team;

            Logger.Get().Info(this,
                $"Received PlayerTeamUpdate for ID: {id}, team: {Enum.GetName(typeof(Team), team)}");

            UpdatePlayerTeam(id, team);

            Ui.UiManager.InfoBox.AddMessage(
                $"Player '{playerTeamUpdate.Username}' is now in Team {Enum.GetName(typeof(Team), team)}");
        }

        /**
         * This will reset the local player's team to be None
         * and will reset all existing player names and hitboxes to be None again too
         */
        public void ResetAllTeams() {
            OnLocalPlayerTeamUpdate(Team.None);

            foreach (var id in _playerData.Keys) {
                UpdatePlayerTeam(id, Team.None);
            }
        }

        private void UpdatePlayerTeam(ushort id, Team team) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Tried to update team for ID {id} while player data did not exists");
                return;
            }

            // Update the team in the player data
            playerData.Team = team;

            // Get the name object and update the color based on the new team
            var nameObject = playerData.PlayerContainer.FindGameObjectInChildren("Username");
            var textMeshObject = nameObject.GetComponent<TextMeshPro>();

            ChangeNameColor(textMeshObject, team);

            // Toggle body damage on if:
            // PvP is enabled and body damage is enabled AND
            // (the teams are not equal or if either doesn't have a team)
            ToggleBodyDamage(
                playerData,
                _gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled &&
                (team != LocalPlayerTeam
                 || team.Equals(Team.None)
                 || LocalPlayerTeam.Equals(Team.None))
            );
        }

        public void OnLocalPlayerTeamUpdate(Team team) {
            LocalPlayerTeam = team;

            var nameObject = HeroController.instance.gameObject.FindGameObjectInChildren("Username");

            var textMeshObject = nameObject.GetComponent<TextMeshPro>();
            ChangeNameColor(textMeshObject, team);

            foreach (var playerData in _playerData.Values) {
                // Toggle body damage on if:
                // PvP is enabled and body damage is enabled AND
                // (the teams are not equal or if either doesn't have a team)
                ToggleBodyDamage(
                    playerData,
                    _gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled &&
                    (playerData.Team != LocalPlayerTeam
                     || playerData.Team.Equals(Team.None)
                     || LocalPlayerTeam.Equals(Team.None))
                );
            }
        }

        public Team GetPlayerTeam(ushort id) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                return Team.None;
            }

            return playerData.Team;
        }

        public void UpdateLocalPlayerSkin(byte skinId) {
            _skinManager.UpdateLocalPlayerSkin(skinId);
        }

        private void OnPlayerSkinUpdate(ClientPlayerSkinUpdate playerSkinUpdate) {
            var id = playerSkinUpdate.Id;
            var skinId = playerSkinUpdate.SkinId;

            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received PlayerSkinUpdate for ID: {id}, skinId: {skinId}");
                return;
            }

            _skinManager.UpdatePlayerSkin(playerData.PlayerObject, skinId);
        }

        public void ResetAllPlayerSkins() {
            // For each registered player, reset their skin
            foreach (var playerData in _playerData.Values) {
                _skinManager.ResetPlayerSkin(playerData.PlayerObject);
            }

            // Also reset our local players skin
            _skinManager.ResetLocalPlayerSkin();
        }

        private void ChangeNameColor(TextMeshPro textMeshObject, Team team) {
            switch (team) {
                case Team.Moss:
                    textMeshObject.color = new Color(0f / 255f, 150f / 255f, 0f / 255f);
                    break;
                case Team.Hive:
                    textMeshObject.color = new Color(200f / 255f, 150f / 255f, 0f / 255f);
                    break;
                case Team.Grimm:
                    textMeshObject.color = new Color(250f / 255f, 50f / 255f, 50f / 255f);
                    break;
                case Team.Lifeblood:
                    textMeshObject.color = new Color(50f / 255f, 150f / 255f, 200f / 255f);
                    break;
                default:
                    textMeshObject.color = Color.white;
                    break;
            }
        }

        public void RemoveNameFromLocalPlayer() {
            RemoveNameFromPlayer(HeroController.instance.gameObject);
        }

        private void RemoveNameFromPlayer(GameObject playerContainer) {
            // Get the name object
            var nameObject = playerContainer.FindGameObjectInChildren("Username");

            // Destroy it if it exists
            if (nameObject != null) {
                Object.Destroy(nameObject);
            }
        }

        public void OnGameSettingsUpdated(bool pvpOrBodyDamageChanged, bool displayNamesChanged) {
            if (pvpOrBodyDamageChanged) {
                // Loop over all player objects
                foreach (var playerData in _playerData.Values) {
                    // Enable the DamageHero component based on whether both PvP and body damage are enabled
                    ToggleBodyDamage(playerData, _gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled);
                }
            }

            if (displayNamesChanged) {
                foreach (var playerData in _playerData.Values) {
                    var nameObject = playerData.PlayerContainer.FindGameObjectInChildren("Username");
                    if (nameObject != null) {
                        nameObject.SetActive(_gameSettings.DisplayNames);
                    }
                }

                var localPlayerObject = HeroController.instance.gameObject;
                if (localPlayerObject != null) {
                    var nameObject = localPlayerObject.FindGameObjectInChildren("Username");
                    if (nameObject != null) {
                        nameObject.SetActive(_gameSettings.DisplayNames);
                    }
                }
            }
        }

        private void ToggleBodyDamage(ClientPlayerData playerData, bool enabled) {
            var playerObject = playerData.PlayerObject;

            // We need to move the player object to the correct layer so it can interact with nail swings etc.
            // Also toggle the enabled state of the DamageHero component
            if (enabled) {
                playerObject.layer = 11;
                playerObject.GetComponent<DamageHero>().enabled = true;
            } else {
                playerObject.layer = 9;
                playerObject.GetComponent<DamageHero>().enabled = false;
            }
        }
    }
}