using System;
using System.Collections.Generic;
using Hkmp.Fsm;
using Hkmp.Game.Client.Skin;
using Hkmp.Game.Settings;
using Hkmp.Networking.Packet;
using Hkmp.Networking.Packet.Data;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using TMPro;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;
using Object = UnityEngine.Object;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client;

/// <summary>
/// Class that manages player objects, spawning and recycling thereof.
/// </summary>
internal class PlayerManager {
    /// <summary>
    /// The name of the game object for the player container prefab.
    /// </summary>
    private const string PlayerContainerPrefabName = "Player Container Prefab";

    /// <summary>
    /// The name of the game object for the player object prefab.
    /// </summary>
    private const string PlayerObjectPrefabName = "Player Prefab";

    /// <summary>
    /// The name (and prefix) of the game object for player containers.
    /// </summary>
    private const string PlayerContainerName = "Player Container";

    /// <summary>
    /// The name of the game object for the username of players.
    /// </summary>
    private const string UsernameObjectName = "Username";

    /// <summary>
    /// The initial size of the pool of player container objects to be pre-instantiated.
    /// </summary>
    private const ushort InitialPoolSize = 64;

    /// <summary>
    /// The current server settings.
    /// </summary>
    private readonly ServerSettings _serverSettings;

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
    /// A queue of pre-instantiated players that will be used when spawning a player.
    /// </summary>
    private readonly Queue<GameObject> _inactivePlayers;

    /// <summary>
    /// The collection of active players spawned from and not in the player pool.
    /// </summary>
    private readonly Dictionary<ushort, GameObject> _activePlayers;

    public PlayerManager(
        PacketManager packetManager,
        ServerSettings serverSettings,
        Dictionary<ushort, ClientPlayerData> playerData
    ) {
        _serverSettings = serverSettings;

        _skinManager = new SkinManager();

        _playerData = playerData;

        _inactivePlayers = new Queue<GameObject>();
        _activePlayers = new Dictionary<ushort, GameObject>();

        On.HeroController.Start += (orig, self) => {
            orig(self);

            if (_playerContainerPrefab == null) {
                CreatePlayerPool();
            }
        };

        // Register packet handlers
        packetManager.RegisterClientPacketHandler<ClientPlayerTeamUpdate>(ClientPacketId.PlayerTeamUpdate,
            OnPlayerTeamUpdate);
        packetManager.RegisterClientPacketHandler<ClientPlayerSkinUpdate>(ClientPacketId.PlayerSkinUpdate,
            OnPlayerSkinUpdate);
    }

    /// <summary>
    /// Create the initial pool of player objects.
    /// </summary>
    private void CreatePlayerPool() {
        // Create a player container prefab, used to spawn players
        _playerContainerPrefab = new GameObject(PlayerContainerPrefabName);

        _playerContainerPrefab.AddComponent<PositionInterpolation>();

        var playerPrefab = new GameObject(PlayerObjectPrefabName,
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

        // Now we need to copy over a lot of variables from the local player object
        var localPlayerObject = HeroController.instance.gameObject;

        // Obtain colliders from both objects
        var collider = playerPrefab.GetComponent<BoxCollider2D>();
        // We're not using the fact that the knight has a BoxCollider as opposed to any other collider
        var localCollider = localPlayerObject.GetComponent<Collider2D>();
        var localColliderBounds = localCollider.bounds;

        // Copy collider offset and size
        collider.isTrigger = true;
        collider.offset = localCollider.offset;
        collider.size = localColliderBounds.size;
        collider.enabled = true;

        // Copy collider bounds
        var bounds = collider.bounds;
        var localBounds = localColliderBounds;
        bounds.min = localBounds.min;
        bounds.max = localBounds.max;

        // Disable non bouncer component
        var nonBouncer = playerPrefab.GetComponent<NonBouncer>();
        nonBouncer.active = false;

        // Add some extra gameObjects related to animation effects
        new GameObject("Attacks") { layer = 9 }.transform.SetParent(playerPrefab.transform);
        new GameObject("Effects") { layer = 9 }.transform.SetParent(playerPrefab.transform);
        new GameObject("Spells") { layer = 9 }.transform.SetParent(playerPrefab.transform);

        playerPrefab.transform.SetParent(_playerContainerPrefab.transform);

        CreateUsername(_playerContainerPrefab);

        _playerContainerPrefab.SetActive(false);
        Object.DontDestroyOnLoad(_playerContainerPrefab);

        // Instantiate all the player objects for the pool
        for (ushort i = 0; i < InitialPoolSize; i++) {
            _inactivePlayers.Enqueue(CreateNewPlayerContainer());
        }
    }

    /// <summary>
    /// Create a new player container object from the <see cref="_playerContainerPrefab"/> prefab.
    /// </summary>
    /// <returns>A new GameObject representing the player container.</returns>
    private GameObject CreateNewPlayerContainer() {
        var playerContainer = Object.Instantiate(_playerContainerPrefab);
        Object.DontDestroyOnLoad(playerContainer);
        playerContainer.name = PlayerContainerName;

        MakeUniqueSpriteAnimator(playerContainer.FindGameObjectInChildren(PlayerObjectPrefabName));

        return playerContainer;
    }

    /// <summary>
    /// Update the position of a player with the given position.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="position">The new position of the player.</param>
    public void UpdatePosition(ushort id, Vector2 position) {
        if (!_playerData.TryGetValue(id, out var playerData) || !playerData.IsInLocalScene) {
            // Logger.Info($"Tried to update position for ID {id} while player data did not exists");
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
            // Logger.Info($"Tried to update scale for ID {id} while player data did not exists");
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
            Logger.Debug($"Tried to get the player data that does not exists for ID {id}");
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
        RecycleAllPlayers();

        // Remove name
        RemoveNameFromLocalPlayer();

        // Reset the skin of the local player
        _skinManager.ResetLocalPlayerSkin();
    }

    /// <summary>
    /// Recycle the player container of the player with the given ID back into the queue.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    public void RecyclePlayer(ushort id) {
        if (!_playerData.TryGetValue(id, out var playerData)) {
            Logger.Debug($"Tried to recycle player that does not exists for ID {id}");
            return;
        }

        RecyclePlayerByData(playerData);
    }

    /// <summary>
    /// Recycle the player container of the player with the given player data.
    /// </summary>
    /// <param name="playerData">The player data of the player.</param>
    private void RecyclePlayerByData(ClientPlayerData playerData) {
        // First reset the player
        ResetPlayer(playerData);

        // Remove the player container from the player data
        playerData.PlayerContainer = null;

        // Find the player container in the active containers if it exists
        if (!_activePlayers.TryGetValue(playerData.Id, out var container)) {
            return;
        }

        container.SetActive(false);
        container.name = PlayerContainerName;

        _activePlayers.Remove(playerData.Id);
        _inactivePlayers.Enqueue(container);
    }

    /// <summary>
    /// Recycle all existing players. <seealso cref="RecyclePlayer"/>
    /// </summary>
    public void RecycleAllPlayers() {
        foreach (var id in _playerData.Keys) {
            // Recycle player
            RecyclePlayer(id);
        }
    }

    /// <summary>
    /// Reset the player with the given player data.
    /// </summary>
    /// <param name="playerData">The player data of the player.</param>
    private void ResetPlayer(ClientPlayerData playerData) {
        var container = playerData.PlayerContainer;
        if (container == null) {
            return;
        }

        ResetPlayerContainer(container);
    }

    /// <summary>
    /// Reset the given player container by removing all game objects not inherent to it.
    /// </summary>
    /// <param name="playerContainer">The game object representing the player container.</param>
    private void ResetPlayerContainer(GameObject playerContainer) {
        // Destroy all descendants and components that weren't originally on the container object
        foreach (Transform child in playerContainer.transform) {
            if (child.name != PlayerObjectPrefabName) {
                continue;
            }

            foreach (Transform grandChild in child) {
                if (grandChild.name is "Attacks" or "Effects" or "Spells") {
                    // Remove all grandchildren from the player prefab's children; there should be none
                    foreach (Transform greatGrandChild in grandChild) {
                        Logger.Debug(
                            $"Destroying child of {grandChild.name}: {greatGrandChild.name}, type: {greatGrandChild.GetType()}");
                        Object.Destroy(greatGrandChild.gameObject);
                    }
                }
            }
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
        // First recycle the player by player data if they have an active container
        RecyclePlayerByData(playerData);

        GameObject playerContainer;

        if (_inactivePlayers.Count <= 0) {
            // Create a new player container
            playerContainer = CreateNewPlayerContainer();
        } else {
            // Dequeue a player container from the inactive players
            playerContainer = _inactivePlayers.Dequeue();
        }

        playerContainer.name = $"{PlayerContainerName} {playerData.Id}";

        _activePlayers[playerData.Id] = playerContainer;

        playerContainer.transform.SetPosition2D(position.X, position.Y);

        var playerObject = playerContainer.FindGameObjectInChildren(PlayerObjectPrefabName);

        SetPlayerObjectBoolScale(playerObject, scale);

        // Set container and children active
        playerContainer.SetActive(true);
        playerContainer.SetActiveChildren(true);

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
            _serverSettings.IsPvpEnabled && _serverSettings.IsBodyDamageEnabled &&
            (team != LocalPlayerTeam
             || team.Equals(Team.None)
             || LocalPlayerTeam.Equals(Team.None))
        );
    }

    /// <summary>
    /// Create a unique copy of a player object's sprite animator so that skins are unique to each player.
    /// </summary>
    /// <param name="playerObject">The player object with the sprite animator component.</param>
    private void MakeUniqueSpriteAnimator(GameObject playerObject) {
        var localPlayer = HeroController.instance;
        // Copy over mesh filter variables
        var meshFilter = playerObject.GetComponent<MeshFilter>();
        var mesh = meshFilter.mesh;
        var localMesh = localPlayer.GetComponent<MeshFilter>().sharedMesh;

        mesh.vertices = localMesh.vertices;
        mesh.normals = localMesh.normals;
        mesh.uv = localMesh.uv;
        mesh.triangles = localMesh.triangles;
        mesh.tangents = localMesh.tangents;

        // Copy mesh renderer material
        var meshRenderer = playerObject.GetComponent<MeshRenderer>();
        meshRenderer.material = new Material(localPlayer.GetComponent<MeshRenderer>().material);

        // Copy over animation library
        var spriteAnimator = playerObject.GetComponent<tk2dSpriteAnimator>();
        // Make a smart copy of the sprite animator library so we can
        // modify the animator without having to worry about other player objects
        spriteAnimator.Library = CopyUtil.SmartCopySpriteAnimation(
            localPlayer.GetComponent<tk2dSpriteAnimator>().Library,
            playerObject
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
        var nameObject = playerContainer.FindGameObjectInChildren(UsernameObjectName);

        if (nameObject == null) {
            nameObject = CreateUsername(playerContainer);
        }

        var textMeshObject = nameObject.GetComponent<TextMeshPro>();

        if (textMeshObject != null) {
            textMeshObject.text = name.ToUpper();
            ChangeNameColor(textMeshObject, team);
        }

        nameObject.SetActive(_serverSettings.DisplayNames);
    }

    /// <summary>
    /// Callback method for when a player team update is received.
    /// </summary>
    /// <param name="playerTeamUpdate">The ClientPlayerTeamUpdate packet data.</param>
    private void OnPlayerTeamUpdate(ClientPlayerTeamUpdate playerTeamUpdate) {
        var id = playerTeamUpdate.Id;
        var team = playerTeamUpdate.Team;

        Logger.Debug($"Received PlayerTeamUpdate for ID: {id}, team: {Enum.GetName(typeof(Team), team)}");

        UpdatePlayerTeam(id, team);
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
            Logger.Debug($"Tried to update team for ID {id} while player data did not exists");
            return;
        }

        // Update the team in the player data
        playerData.Team = team;

        if (!playerData.IsInLocalScene) {
            return;
        }

        // Get the name object and update the color based on the new team
        var nameObject = playerData.PlayerContainer.FindGameObjectInChildren(UsernameObjectName);
        var textMeshObject = nameObject.GetComponent<TextMeshPro>();

        ChangeNameColor(textMeshObject, team);

        // Toggle body damage on if:
        // PvP is enabled and body damage is enabled AND
        // (the teams are not equal or if either doesn't have a team)
        ToggleBodyDamage(
            playerData,
            _serverSettings.IsPvpEnabled && _serverSettings.IsBodyDamageEnabled &&
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

        var nameObject = HeroController.instance.gameObject.FindGameObjectInChildren(UsernameObjectName);

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
                _serverSettings.IsPvpEnabled && _serverSettings.IsBodyDamageEnabled &&
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
            Logger.Debug($"Received PlayerSkinUpdate for ID: {id}, skinId: {skinId}");
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
        if (HeroController.instance != null) {
            RemoveNameFromPlayer(HeroController.instance.gameObject);
        }
    }

    /// <summary>
    /// Remove the name of a given player container.
    /// </summary>
    /// <param name="playerContainer">The GameObject for the player container.</param>
    private void RemoveNameFromPlayer(GameObject playerContainer) {
        // Get the name object
        var nameObject = playerContainer.FindGameObjectInChildren(UsernameObjectName);

        // Deactivate it if it exists
        if (nameObject != null) {
            nameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Callback method for when the server settings are updated.
    /// </summary>
    /// <param name="pvpOrBodyDamageChanged">Whether the PvP or body damage settings changed.</param>
    /// <param name="displayNamesChanged">Whether the display names setting changed.</param>
    public void OnServerSettingsUpdated(bool pvpOrBodyDamageChanged, bool displayNamesChanged) {
        if (pvpOrBodyDamageChanged) {
            // Loop over all player objects
            foreach (var playerData in _playerData.Values) {
                if (playerData.IsInLocalScene) {
                    // Enable the DamageHero component based on whether both PvP and body damage are enabled
                    ToggleBodyDamage(playerData, _serverSettings.IsPvpEnabled && _serverSettings.IsBodyDamageEnabled);
                }
            }
        }

        if (displayNamesChanged) {
            foreach (var playerData in _playerData.Values) {
                var nameObject = playerData.PlayerContainer.FindGameObjectInChildren(UsernameObjectName);
                if (nameObject != null) {
                    nameObject.SetActive(_serverSettings.DisplayNames);
                }
            }

            var localPlayerObject = HeroController.instance.gameObject;
            if (localPlayerObject != null) {
                var nameObject = localPlayerObject.FindGameObjectInChildren(UsernameObjectName);
                if (nameObject != null) {
                    nameObject.SetActive(_serverSettings.DisplayNames);
                }
            }
        }
    }

    /// <summary>
    /// Create a new username object and add it as a child of a player container.
    /// </summary>
    /// <param name="playerContainer">The player container to add the username object as a child of.</param>
    /// <returns>The new GameObject that was created for the username.</returns>
    private GameObject CreateUsername(GameObject playerContainer) {
        var nameObject = new GameObject(UsernameObjectName);

        nameObject.transform.position = playerContainer.transform.position + Vector3.up * 1.25f;
        nameObject.transform.SetParent(playerContainer.transform);
        nameObject.transform.localScale = new Vector3(0.25f, 0.25f, nameObject.transform.localScale.z);

        nameObject.AddComponent<KeepWorldScalePositive>();

        // Add a TextMeshPro component to it, so we can render text
        var textMeshObject = nameObject.AddComponent<TextMeshPro>();
        textMeshObject.text = UsernameObjectName;
        textMeshObject.alignment = TextAlignmentOptions.Center;
        textMeshObject.font = FontManager.InGameNameFont;
        textMeshObject.fontSize = 22;
        textMeshObject.outlineWidth = 0.2f;
        textMeshObject.outlineColor = Color.black;

        nameObject.transform.SetParent(playerContainer.transform);

        return nameObject;
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
