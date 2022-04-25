using Hkmp.Fsm;
using Hkmp.Game.Client.Skin;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client
{
    /// <summary>
    /// Class that manages player objects, spawning and recycling thereof.
    /// </summary>
    internal class PlayerManager {
        /// <summary>
        /// The current game settings.
        /// </summary>
        private readonly Settings.GameSettings _gameSettings;
        /// <summary>
        /// The skin manager instance.
        /// </summary>
        private readonly SkinManager _skinManager;

        /// <summary>
        /// Reference to the client player data dictionary (<see cref="ClientManager._playerData"/>)
        /// from <see cref="ClientManager"/>.
        /// </summary>
        private readonly Dictionary<ushort, ClientPlayerData> _playerData;

        /// <summary>
        /// The team that our local player is on.
        /// </summary>
        public Team LocalPlayerTeam { get; private set; } = Team.None;

        /// <summary>
        /// The player container prefab GameObject.
        /// </summary>
        private GameObject _playerContainerPrefab;

        /// <summary>
        /// A collection of pre-instantiated players that will be fetched when spawning a player.
        /// </summary>
        private Dictionary<ushort, GameObject> _players = new();

        /// <summary>
        /// The initial size of the pool of player container objects to be pre-instantiated.
        /// </summary>
        private const ushort InitialPoolSize = 255;

        private readonly List<Type> _usernameComponentTypes = new();

        private readonly List<Type> _prefabComponentTypes = new() {
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
        };

        public PlayerManager(
            PacketManager packetManager, 
            Settings.GameSettings gameSettings,
            Dictionary<ushort, ClientPlayerData> playerData
        ) {
            _gameSettings = gameSettings;

            _skinManager = new SkinManager();

            _playerData = playerData;

            global::GameManager.instance.StartCoroutine(CreatePlayerPool());

            // Register packet handlers
            packetManager.RegisterClientPacketHandler<ClientPlayerTeamUpdate>(ClientPacketId.PlayerTeamUpdate,
                OnPlayerTeamUpdate);
            packetManager.RegisterClientPacketHandler<ClientPlayerSkinUpdate>(ClientPacketId.PlayerSkinUpdate,
                OnPlayerSkinUpdate);
        }

        private IEnumerator CreatePlayerPool() {
            // Create a player container prefab, used to spawn players
            _playerContainerPrefab = new GameObject("Player Container");

            _playerContainerPrefab.AddComponent<PositionInterpolation>();

            var playerPrefab = new GameObject("PlayerPrefab") {
                layer = 9
            };

            foreach (var componentType in _prefabComponentTypes) {
                playerPrefab.AddComponent(componentType);
            }

            // Now we need to copy over a lot of variables from the local player object
            yield return new WaitUntil(() => HeroController.instance != null);
            var localPlayerObject = HeroController.instance.gameObject;
            
            // Obtain colliders from both objects
            var collider = playerPrefab.GetComponent<BoxCollider2D>();
            // We're not using the fact that the knight has a BoxCollider as opposed to any other collider
            var localCollider = localPlayerObject.GetComponent<Collider2D>();
            
            // Copy collider offset and size
            collider.isTrigger = true;
            collider.offset = localCollider.offset;
            collider.size = localCollider.bounds.size;
            collider.enabled = true;
            
            // Copy collider bounds
            var bounds = collider.bounds;
            var localBounds = localCollider.bounds;
            bounds.min = localBounds.min;
            bounds.max = localBounds.max;
            
            // Add some extra gameObjects related to animation effects
            new GameObject("Attacks") { layer = 9 }.transform.SetParent(playerPrefab.transform);
            new GameObject("Effects") { layer = 9 }.transform.SetParent(playerPrefab.transform);
            new GameObject("Spells") { layer = 9 }.transform.SetParent(playerPrefab.transform);
            
            // Copy over mesh filter variables
            var meshFilter = playerPrefab.GetComponent<MeshFilter>();
            var mesh = meshFilter.mesh;
            var localMesh = localPlayerObject.GetComponent<MeshFilter>().sharedMesh;
            
            mesh.vertices = localMesh.vertices;
            mesh.normals = localMesh.normals;
            mesh.uv = localMesh.uv;
            mesh.triangles = localMesh.triangles;
            mesh.tangents = localMesh.tangents;

            // Copy mesh renderer material
            var meshRenderer = playerPrefab.GetComponent<MeshRenderer>();
            meshRenderer.material = new Material(localPlayerObject.GetComponent<MeshRenderer>().material);
            
            // Disable non bouncer component
            var nonBouncer = playerPrefab.GetComponent<NonBouncer>();
            nonBouncer.active = false;
            
            // Copy over animation library
            var spriteAnimator = playerPrefab.GetComponent<tk2dSpriteAnimator>();
            // Make a smart copy of the sprite animator library so we can
            // modify the animator without having to worry about other player objects
            spriteAnimator.Library = CopyUtil.SmartCopySpriteAnimation(
                localPlayerObject.GetComponent<tk2dSpriteAnimator>().Library,
                playerPrefab
            );
            
            playerPrefab.transform.SetParent(_playerContainerPrefab.transform);
            
            var nameObject = new GameObject("Username");
            nameObject.transform.position = _playerContainerPrefab.transform.position + Vector3.up * 1.25f;
            nameObject.name = "Username";
            nameObject.transform.SetParent(_playerContainerPrefab.transform);
            nameObject.transform.localScale = new Vector3(0.25f, 0.25f, nameObject.transform.localScale.z);
            var keepScalePositive = nameObject.AddComponent<KeepWorldScalePositive>();
            _usernameComponentTypes.Add(keepScalePositive.GetType());

            // Add a TextMeshPro component to it, so we can render text
            var textMeshObject = nameObject.AddComponent<TextMeshPro>();
            textMeshObject.text = "Username";
            textMeshObject.alignment = TextAlignmentOptions.Center;
            textMeshObject.font = FontManager.InGameNameFont;
            textMeshObject.fontSize = 22;
            textMeshObject.outlineWidth = 0.2f;
            textMeshObject.outlineColor = Color.black;
            _usernameComponentTypes.Add(textMeshObject.GetType());

            _playerContainerPrefab.SetActive(false);
            Object.DontDestroyOnLoad(_playerContainerPrefab);

            for (ushort playerId = 0; playerId <= InitialPoolSize; playerId++) {
                var playerContainer = Object.Instantiate(_playerContainerPrefab);
                playerContainer.name += $" {playerId}";
                Object.DontDestroyOnLoad(playerContainer);

                _players.Add(playerId, playerContainer);
            }
        }

        /// <summary>
        /// Update the position of a player with the given position.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <param name="position">The new position of the player.</param>
        public void UpdatePosition(ushort id, Vector2 position) {
            if (!_playerData.TryGetValue(id, out var playerData) || !playerData.IsInLocalScene) {
                // Logger.Get().Warn(this, $"Tried to update position for ID {id} while player data did not exists");
                return;
            }

            var playerContainer = playerData.PlayerContainer;
            if (playerContainer != null) {
                var unityPosition = new Vector3(position.X, position.Y);

                playerContainer.GetComponent<PositionInterpolation>().SetNewPosition(unityPosition);
            }
        }

        /// <summary>
        /// Update the scale of a player with the given boolean.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <param name="scale">The new scale as a boolean, true indicating a X scale of 1,
        /// false indicating a X scale of -1.</param>
        public void UpdateScale(ushort id, bool scale) {
            if (!_playerData.TryGetValue(id, out var playerData) || !playerData.IsInLocalScene) {
                // Logger.Get().Warn(this, $"Tried to update scale for ID {id} while player data did not exists");
                return;
            }

            var playerObject = playerData.PlayerObject;
            SetPlayerObjectBoolScale(playerObject, scale);
        }

        /// <summary>
        /// Sets the scale of a player object from a boolean.
        /// </summary>
        /// <param name="playerObject">The GameObject representing the player.</param>
        /// <param name="scale">The new scale as a boolean, true indicating a X scale of 1,
        /// false indicating a X scale of -1.</param>
        private void SetPlayerObjectBoolScale(GameObject playerObject, bool scale) {
            if (playerObject == null) {
                return;
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

        /// <summary>
        /// Get the player object given the player ID.
        /// </summary>
        /// <param name="id">The player ID.</param>
        /// <returns>The GameObject for the player.</returns>
        public GameObject GetPlayerObject(ushort id) {
            if (!_playerData.TryGetValue(id, out var playerData) || !playerData.IsInLocalScene) {
                Logger.Get().Error(this, $"Tried to get the player data that does not exists for ID {id}");
                return null;
            }

            return playerData.PlayerObject;
        }

        /// <summary>
        /// Callback method for when the local user disconnects. Will reset all player related things
        /// to their default values.
        /// </summary>
        public void OnDisconnect() {
            // Reset the local player's team
            LocalPlayerTeam = Team.None;

            // Clear all players
            ResetAllPlayers();
            RecycleAllPlayers();

            // Remove name
            RemoveNameFromLocalPlayer();

            // Reset the skin of the local player
            _skinManager.ResetLocalPlayerSkin();
        }

        // and only destroy when player left server
        /// <summary>
        /// Destroy the player with the given ID.
        /// </summary>
        /// <param name="id">The player ID.</param>
        public void RecyclePlayer(ushort id) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Tried to recycle player that does not exists for ID {id}");
                return;
            }

            // Recycle gameObject
            var container = playerData.PlayerContainer;
            container?.SetActive(false);
        }

        /// <summary>
        /// Recycle all existing players.
        /// </summary>
        public void RecycleAllPlayers() {
            foreach (var (id, playerData) in _playerData) {
                // Recycle player
                RecyclePlayer(id);
            }
        }

        /// <summary>
        /// Reset the player with the given ID to its initial state.
        /// </summary>
        /// <param name="id">The player ID.</param>
        public void ResetPlayer(ushort id) {
            if (!_playerData.TryGetValue(id, out var playerData))
            {
                Logger.Get().Warn(this, $"Tried to reset player that does not exists for ID {id}");
                return;
            }

            var container = playerData.PlayerContainer;
            if (container is null) return;

            // Destroy all descendants and components that weren't originally on the container object.
            foreach (Transform child in container.transform)
            {
                switch (child.name)
                {
                    case "PlayerPrefab":
                        foreach (var component in child.GetComponents<Component>())
                        {
                            if (!_prefabComponentTypes.Contains(component.GetType()))
                            {
                                Object.Destroy(component);
                            }
                        }

                        foreach (Transform grandChild in child)
                        {
                            if (grandChild.name is "Attacks" or "Effects" or "Spells")
                            {
                                foreach (var component in grandChild.GetComponents<Component>())
                                {
                                    Object.Destroy(component);
                                }

                                foreach (Transform greatGrandChild in grandChild)
                                {
                                    Object.Destroy(greatGrandChild.gameObject);
                                }
                            }
                            else
                            {
                                Object.Destroy(grandChild.gameObject);
                            }
                        }

                        break;
                    case "Username":
                        foreach (var component in child.GetComponents<Component>())
                        {
                            if (!_usernameComponentTypes.Contains(component.GetType()))
                            {
                                Object.Destroy(component);
                            }
                        }

                        foreach (Transform grandChild in child)
                        {
                            Object.Destroy(grandChild.gameObject);
                        }

                        break;
                    default:
                        Object.Destroy(child);
                        break;
                }
            }
        }

        /// <summary>
        /// Reset all existing players.
        /// </summary>
        public void ResetAllPlayers()
        {
            foreach (var (id, playerData) in _playerData)
            {
                // Recycle player
                ResetPlayer(id);
            }
        }

        /// <summary>
        /// Spawn a new player object with the given data.
        /// </summary>
        /// <param name="playerData">The client player data for the player.</param>
        /// <param name="name">The username of the player.</param>
        /// <param name="position">The Vector2 denoting the position of the player.</param>
        /// <param name="scale">The boolean representing the scale of the player.</param>
        /// <param name="team">The team the player is on.</param>
        /// <param name="skinId">The ID of the skin the player is using.</param>
        public void SpawnPlayer(
            ClientPlayerData playerData,
            string name,
            Vector2 position,
            bool scale,
            Team team,
            byte skinId
        ) {
            GameObject playerContainer;
            if (!_players.ContainsKey(playerData.Id)) {
                // Create a player container with the player ID
                playerContainer = Object.Instantiate(_playerContainerPrefab);
                playerContainer.name += $" {playerData.Id}";
                _players.Add(playerData.Id, playerContainer);
            } else {
                // Fetch the player container according to player ID
                playerContainer = _players[playerData.Id];
            }

            playerContainer.transform.SetPosition2D(position.X, position.Y);

            var playerObject = playerContainer.FindGameObjectInChildren("PlayerPrefab");

            SetPlayerObjectBoolScale(playerObject, scale);

            // Set container and children active
            playerContainer.SetActive(true);
            playerContainer.SetActiveChildren(true);

            // Disable DamageHero component unless pvp is enabled
            if (_gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled) {
                playerObject.layer = 11;
                playerObject.GetComponent<DamageHero>().enabled = true;
            } else {
                playerObject.layer = 9;
                playerObject.GetComponent<DamageHero>().enabled = false;
            }
            
            AddNameToPlayer(playerContainer, name, team);

            // Let the SkinManager update the skin
            _skinManager.UpdatePlayerSkin(playerObject, skinId);

            // Store the player data
            playerData.PlayerContainer = playerContainer;
            playerData.PlayerObject = playerObject;
            playerData.Team = team;
            playerData.SkinId = skinId;

            // Set whether this player should have body damage
            // Only if:
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

        /// <summary>
        /// Add a name to the given player container object.
        /// </summary>
        /// <param name="playerContainer">The GameObject for the player container.</param>
        /// <param name="name">The username that the object should have.</param>
        /// <param name="team">The team that the player is on.</param>
        public void AddNameToPlayer(GameObject playerContainer, string name, Team team = Team.None) {
            // Create a name object to set the username to, slightly above the player object
            var nameObject = playerContainer.FindGameObjectInChildren("Username");
            TextMeshPro textMeshObject;

            if (nameObject == null) {
                nameObject = new GameObject("Username");
                nameObject.transform.position = playerContainer.transform.position + Vector3.up * 1.25f;
                nameObject.name = "Username";
                nameObject.transform.SetParent(playerContainer.transform);
                nameObject.transform.localScale = new Vector3(0.25f, 0.25f, nameObject.transform.localScale.z);
                nameObject.AddComponent<KeepWorldScalePositive>();

                // Add a TextMeshPro component to it, so we can render text
                textMeshObject = nameObject.AddComponent<TextMeshPro>();
                textMeshObject.text = "Username";
                textMeshObject.alignment = TextAlignmentOptions.Center;
                textMeshObject.font = FontManager.InGameNameFont;
                textMeshObject.fontSize = 22;
                textMeshObject.outlineWidth = 0.2f;
                textMeshObject.outlineColor = Color.black;

                nameObject.transform.SetParent(playerContainer.transform);
            } else {
                textMeshObject = nameObject.GetComponent<TextMeshPro>();
            }

            if (textMeshObject != null) {
                textMeshObject.text = name.ToUpper();
                ChangeNameColor(textMeshObject, team);
            }

            nameObject.SetActive(_gameSettings.DisplayNames);
        }

        /// <summary>
        /// Callback method for when a player team update is received.
        /// </summary>
        /// <param name="playerTeamUpdate">The ClientPlayerTeamUpdate packet data.</param>
        private void OnPlayerTeamUpdate(ClientPlayerTeamUpdate playerTeamUpdate) {
            var id = playerTeamUpdate.Id;
            var team = playerTeamUpdate.Team;

            Logger.Get().Info(this,
                $"Received PlayerTeamUpdate for ID: {id}, team: {Enum.GetName(typeof(Team), team)}");

            UpdatePlayerTeam(id, team);

            // UiManager.InternalInfoBox.AddMessage(
            //     $"Player '{playerTeamUpdate.Username}' is now in Team {Enum.GetName(typeof(Team), team)}");
        }

        /// <summary>
        /// Reset the local player's team to be None and reset all existing player names and hit-boxes.
        /// </summary>
        public void ResetAllTeams() {
            OnLocalPlayerTeamUpdate(Team.None);

            foreach (var id in _playerData.Keys) {
                UpdatePlayerTeam(id, Team.None);
            }
        }

        /// <summary>
        /// Update the team of a player.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <param name="team">The team that the player should have.</param>
        private void UpdatePlayerTeam(ushort id, Team team) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Tried to update team for ID {id} while player data did not exists");
                return;
            }

            // Update the team in the player data
            playerData.Team = team;
            
            if (!playerData.IsInLocalScene) {
                return;
            }

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

        /// <summary>
        /// Callback method for when the team of the local player updates.
        /// </summary>
        /// <param name="team">The new team of the local player.</param>
        public void OnLocalPlayerTeamUpdate(Team team) {
            LocalPlayerTeam = team;

            var nameObject = HeroController.instance.gameObject.FindGameObjectInChildren("Username");

            var textMeshObject = nameObject.GetComponent<TextMeshPro>();
            ChangeNameColor(textMeshObject, team);

            foreach (var playerData in _playerData.Values) {
                if (!playerData.IsInLocalScene) {
                    continue;
                }
                
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

        /// <summary>
        /// Get the team of a player.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <returns>The team of the player.</returns>
        public Team GetPlayerTeam(ushort id) {
            if (!_playerData.TryGetValue(id, out var playerData)) {
                return Team.None;
            }

            return playerData.Team;
        }

        /// <summary>
        /// Update the skin of the local player.
        /// </summary>
        /// <param name="skinId">The ID of the skin to update to.</param>
        public void UpdateLocalPlayerSkin(byte skinId) {
            _skinManager.UpdateLocalPlayerSkin(skinId);
        }

        /// <summary>
        /// Callback method for when a player updates their skin.
        /// </summary>
        /// <param name="playerSkinUpdate">The ClientPlayerSkinUpdate packet data.</param>
        private void OnPlayerSkinUpdate(ClientPlayerSkinUpdate playerSkinUpdate) {
            var id = playerSkinUpdate.Id;
            var skinId = playerSkinUpdate.SkinId;

            if (!_playerData.TryGetValue(id, out var playerData)) {
                Logger.Get().Warn(this, $"Received PlayerSkinUpdate for ID: {id}, skinId: {skinId}");
                return;
            }

            playerData.SkinId = skinId;

            _skinManager.UpdatePlayerSkin(playerData.PlayerObject, skinId);
        }

        /// <summary>
        /// Reset the skins of all players.
        /// </summary>
        public void ResetAllPlayerSkins() {
            // For each registered player, reset their skin
            foreach (var playerData in _playerData.Values) {
                _skinManager.ResetPlayerSkin(playerData.PlayerObject);
            }

            // Also reset our local players skin
            _skinManager.ResetLocalPlayerSkin();
        }

        /// <summary>
        /// Change the color of a TextMeshPro object according to the team.
        /// </summary>
        /// <param name="textMeshObject">The TextMeshPro object representing the name.</param>
        /// <param name="team">The team that the name should be colored after.</param>
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

        /// <summary>
        /// Remove the name from the local player.
        /// </summary>
        private void RemoveNameFromLocalPlayer() {
            RemoveNameFromPlayer(HeroController.instance.gameObject);
        }

        /// <summary>
        /// Remove the name of a given player container.
        /// </summary>
        /// <param name="playerContainer">The GameObject for the player container.</param>
        private void RemoveNameFromPlayer(GameObject playerContainer) {
            // Get the name object
            var nameObject = playerContainer.FindGameObjectInChildren("Username");

            // Deactivate it if it exists
            if (nameObject != null) {
                nameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Callback method for when the game settings are updated.
        /// </summary>
        /// <param name="pvpOrBodyDamageChanged">Whether the PvP or body damage settings changed.</param>
        /// <param name="displayNamesChanged">Whether the display names setting changed.</param>
        public void OnGameSettingsUpdated(bool pvpOrBodyDamageChanged, bool displayNamesChanged) {
            if (pvpOrBodyDamageChanged) {
                // Loop over all player objects
                foreach (var playerData in _playerData.Values) {
                    if (playerData.IsInLocalScene) {
                        // Enable the DamageHero component based on whether both PvP and body damage are enabled
                        ToggleBodyDamage(playerData, _gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled);
                    }
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

        /// <summary>
        /// Toggle the body damage for the given player data.
        /// </summary>
        /// <param name="playerData">The client player data.</param>
        /// <param name="enabled">Whether body damage is enabled.</param>
        private void ToggleBodyDamage(ClientPlayerData playerData, bool enabled) {
            var playerObject = playerData.PlayerObject;
            if (playerObject == null) {
                return;
            }

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