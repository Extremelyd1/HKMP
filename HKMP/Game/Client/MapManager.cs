using System.Collections.Generic;
using Hkmp.Api.Client;
using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Util;
using Modding;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client;

/// <summary>
/// A class that manages player locations on the in-game map.
/// </summary>
internal class MapManager : IMapManager {
    /// <summary>
    /// The net client instance.
    /// </summary>
    private readonly NetClient _netClient;

    /// <summary>
    /// The current server settings.
    /// </summary>
    private readonly ServerSettings _serverSettings;

    /// <summary>
    /// Dictionary containing map icon objects per player ID.
    /// </summary>
    private readonly Dictionary<ushort, PlayerMapEntry> _mapEntries;

    /// <summary>
    /// The last sent map position.
    /// </summary>
    private Vector3 _lastPosition;

    /// <summary>
    /// The value of the last sent whether the map icon was active. If true, we have sent to the server
    /// that we have a map icon active. Otherwise, we have sent to the server that we don't have a map
    /// icon active.
    /// </summary>
    private bool _lastSentMapIcon;

    /// <summary>
    /// Whether we should display the map icons. True if the map is opened, false otherwise.
    /// </summary>
    private bool _displayingIcons;

    public MapManager(NetClient netClient, ServerSettings serverSettings) {
        _netClient = netClient;
        _serverSettings = serverSettings;

        _mapEntries = new Dictionary<ushort, PlayerMapEntry>();

        _netClient.DisconnectEvent += OnDisconnect;

        // Register a hero controller update callback, so we can update the map icon position
        On.HeroController.Update += HeroControllerOnUpdate;

        // Register when the player closes their map, so we can hide the icons
        On.GameMap.CloseQuickMap += OnCloseQuickMap;

        // Register when the player opens their map, which is when the compass position is calculated 
        On.GameMap.PositionCompass += OnPositionCompass;
    }

    /// <summary>
    /// Callback method for the HeroController#Update method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The HeroController instance.</param>
    private void HeroControllerOnUpdate(On.HeroController.orig_Update orig, HeroController self) {
        // Execute the original method
        orig(self);

        // If we are not connect, we don't have to send anything
        if (!_netClient.IsConnected) {
            return;
        }

        // Check whether the player has a map location for an icon
        var hasMapLocation = TryGetMapLocation(out var newPosition);

        // Whether we have a map icon active
        var hasMapIcon = hasMapLocation;
        if (!_serverSettings.AlwaysShowMapIcons) {
            if (!_serverSettings.OnlyBroadcastMapIconWithWaywardCompass) {
                hasMapIcon = false;
            } else {
                // We do not always show map icons, but only when we are wearing wayward compass
                // So we need to check whether we are wearing wayward compass
                if (!PlayerData.instance.GetBool(nameof(PlayerData.equippedCharm_2))) {
                    hasMapIcon = false;
                }
            }
        }

        if (hasMapIcon != _lastSentMapIcon) {
            _lastSentMapIcon = hasMapIcon;

            _netClient.UpdateManager.UpdatePlayerMapIcon(hasMapIcon);

            // If we don't have a map icon anymore, we reset the last position so that
            // if we have an icon again, we will immediately also send a map position update
            if (!hasMapIcon) {
                _lastPosition = Vector3.zero;
            }
        }

        // If we don't currently have a map icon active or if we are in a scene transition,
        // we don't send map position updates
        if (!hasMapIcon || global::GameManager._instance.IsInSceneTransition) {
            return;
        }

        // Only send update if the position changed
        if (newPosition != _lastPosition) {
            var vec2 = new Vector2(newPosition.x, newPosition.y);

            _netClient.UpdateManager.UpdatePlayerMapPosition(vec2);

            // Update the last position, since it changed
            _lastPosition = newPosition;
        }
    }

    /// <summary>
    /// Try to get the current map location of the local player.
    /// </summary>
    /// <param name="mapLocation">A Vector3 representing the map location or the zero vector if the map location could not be found.</param>
    /// <returns>true if the map location could be found; false otherwise.</returns>
    private bool TryGetMapLocation(out Vector3 mapLocation) {
        // Set the default value for the map location
        mapLocation = Vector3.zero;

        // Get the game manager instance
        var gameManager = global::GameManager.instance;
        // Get the current map zone of the game manager and check whether we are in
        // an area that doesn't shop up on the map
        var currentMapZone = gameManager.GetCurrentMapZone();

        // Get the game map instance
        var gameMap = GetGameMap();
        if (gameMap == null) {
            return false;
        }

        // This is what the PositionCompass method in GameMap calculates to determine
        // the compass icon location
        // We mimic it, because we need it to always update instead of only when the map is open
        string sceneName;
        if (gameMap.inRoom) {
            currentMapZone = gameMap.doorMapZone;
            sceneName = gameMap.doorScene;
        } else {
            sceneName = gameManager.sceneName;
        }

        GameObject sceneObject = null;
        var areaObject = GetAreaObjectByName(gameMap, currentMapZone);

        if (areaObject == null) {
            return false;
        }

        for (var i = 0; i < areaObject.transform.childCount; i++) {
            var childObject = areaObject.transform.GetChild(i).gameObject;
            if (childObject.name.Equals(sceneName)) {
                sceneObject = childObject;
                break;
            }
        }

        if (sceneObject == null) {
            return false;
        }

        var sceneObjectPos = sceneObject.transform.localPosition;
        var areaObjectPos = areaObject.transform.localPosition;

        var currentScenePos = new Vector3(
            sceneObjectPos.x + areaObjectPos.x,
            sceneObjectPos.y + areaObjectPos.y,
            0f
        );

        var size = sceneObject.GetComponent<SpriteRenderer>().sprite.bounds.size;

        Vector3 position;

        if (gameMap.inRoom) {
            position = new Vector3(
                currentScenePos.x - size.x / 2.0f + (gameMap.doorX + gameMap.doorOriginOffsetX) /
                gameMap.doorSceneWidth *
                size.x,
                currentScenePos.y - size.y / 2.0f + (gameMap.doorY + gameMap.doorOriginOffsetY) /
                gameMap.doorSceneHeight *
                size.y,
                -1f
            );
        } else {
            var playerPosition = HeroController.instance.gameObject.transform.position;

            var originOffsetX = ReflectionHelper.GetField<GameMap, float>(gameMap, "originOffsetX");
            var originOffsetY = ReflectionHelper.GetField<GameMap, float>(gameMap, "originOffsetY");
            var sceneWidth = ReflectionHelper.GetField<GameMap, float>(gameMap, "sceneWidth");
            var sceneHeight = ReflectionHelper.GetField<GameMap, float>(gameMap, "sceneHeight");

            position = new Vector3(
                currentScenePos.x - size.x / 2.0f + (playerPosition.x + originOffsetX) / sceneWidth *
                size.x,
                currentScenePos.y - size.y / 2.0f + (playerPosition.y + originOffsetY) / sceneHeight *
                size.y,
                -1f
            );
        }

        mapLocation = position;
        return true;
    }

    /// <summary>
    /// Update whether the given player has an active map icon.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="hasMapIcon">Whether the player has an active map icon.</param>
    public void UpdatePlayerHasIcon(ushort id, bool hasMapIcon) {
        // If there does not exist an entry for this ID yet, we create it
        if (!_mapEntries.TryGetValue(id, out var mapEntry)) {
            _mapEntries[id] = mapEntry = new PlayerMapEntry();
        }

        if (mapEntry.HasMapIcon) {
            if (!hasMapIcon) {
                // If the player had an active map icon, but we receive that they do not anymore
                // we destroy the map icon object if it exists
                if (mapEntry.GameObject != null) {
                    Object.Destroy(mapEntry.GameObject);
                }
            }
        } else {
            if (hasMapIcon) {
                // If the player did not have an active map icon, but we receive that they do we
                // create an icon
                CreatePlayerIcon(id, mapEntry.Position);
            }
        }

        mapEntry.HasMapIcon = hasMapIcon;
    }

    /// <summary>
    /// Update the map icon of a given player with the given position.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="position">The new position on the map.</param>
    public void UpdatePlayerIcon(ushort id, Vector2 position) {
        // If there does not exist an entry for this id yet, we create it
        if (!_mapEntries.TryGetValue(id, out var mapEntry)) {
            _mapEntries[id] = mapEntry = new PlayerMapEntry();
        }

        // Always store the position in case we later get an active map icon without position
        mapEntry.Position = position;

        // If the player does not have an active map icon
        if (!mapEntry.HasMapIcon) {
            return;
        }

        // Check whether the object still exists
        var mapObject = mapEntry.GameObject;
        if (mapObject == null) {
            CreatePlayerIcon(id, position);
            return;
        }

        // Check if the transform is still valid and otherwise destroy the object
        // This is possible since whenever we receive a new update packet, we
        // will just create a new map icon
        var transform = mapObject.transform;
        if (transform == null) {
            Object.Destroy(mapObject);
            return;
        }

        var unityPosition = new Vector3(
            position.X,
            position.Y,
            transform.localPosition.z
        );

        // Update the position of the player icon
        transform.localPosition = unityPosition;
    }

    /// <summary>
    /// Callback method on the GameMap#CloseQuickMap method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The GameMap instance.</param>
    private void OnCloseQuickMap(On.GameMap.orig_CloseQuickMap orig, GameMap self) {
        orig(self);

        // We have closed the map, so we can disable the icons
        _displayingIcons = false;
        UpdateMapIconsActive();
    }

    /// <summary>
    /// Callback method on the GameMap#PositionCompass method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The GameMap instance.</param>
    /// <param name="posShade">The boolean value whether to position the shade.</param>
    private void OnPositionCompass(On.GameMap.orig_PositionCompass orig, GameMap self, bool posShade) {
        orig(self, posShade);

        var posGate = ReflectionHelper.GetField<GameMap, bool>(self, "posGate");

        // If this is a call where we either update the shade position or the dream gate position,
        // we don't want to display the icons again, because we haven't opened the map
        if (posShade || posGate) {
            return;
        }

        // Otherwise, we have opened the map
        _displayingIcons = true;
        UpdateMapIconsActive();
    }

    /// <summary>
    /// Update all existing map icons based on whether they should be active according to server settings.
    /// </summary>
    private void UpdateMapIconsActive() {
        foreach (var mapEntry in _mapEntries.Values) {
            if (mapEntry.HasMapIcon && mapEntry.GameObject != null) {
                mapEntry.GameObject.SetActive(_displayingIcons);
            }
        }
    }

    /// <summary>
    /// Create a map icon for a player and store it in the mapping.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    /// <param name="position">The position of the map icon.</param>
    private void CreatePlayerIcon(ushort id, Vector2 position) {
        if (!_mapEntries.TryGetValue(id, out var mapEntry)) {
            return;
        }

        var gameMap = GetGameMap();
        if (gameMap == null) {
            return;
        }

        var compassIconPrefab = gameMap.compassIcon;
        if (compassIconPrefab == null) {
            Logger.Warn("CompassIcon prefab is null");
            return;
        }

        // Create a new player icon relative to the game map
        var mapIcon = Object.Instantiate(
            compassIconPrefab,
            gameMap.gameObject.transform
        );
        mapIcon.SetActive(_displayingIcons);

        var unityPosition = new Vector3(
            position.X,
            position.Y,
            compassIconPrefab.transform.localPosition.z
        );

        // Set the position of the player icon
        mapIcon.transform.localPosition = unityPosition;

        // Remove the bob effect when walking with the map
        Object.Destroy(mapIcon.LocateMyFSM("Mapwalk Bob"));

        // Put it in the list
        mapEntry.GameObject = mapIcon;
    }

    /// <summary>
    /// Remove a map entry for a player. For example, if they disconnect from the server.
    /// </summary>
    /// <param name="id">The ID of the player.</param>
    public void RemoveEntryForPlayer(ushort id) {
        if (_mapEntries.TryGetValue(id, out var mapEntry)) {
            if (mapEntry.GameObject != null) {
                Object.Destroy(mapEntry.GameObject);
            }

            _mapEntries.Remove(id);
        }
    }

    /// <summary>
    /// Remove all map icons.
    /// </summary>
    public void RemoveAllIcons() {
        // Destroy all existing map icons
        foreach (var mapEntry in _mapEntries.Values) {
            if (mapEntry.GameObject != null) {
                Object.Destroy(mapEntry.GameObject);
            }
        }
    }

    /// <summary>
    /// Callback method for when the local user disconnects.
    /// </summary>
    private void OnDisconnect() {
        RemoveAllIcons();

        _mapEntries.Clear();

        // Reset variables to their initial values
        _lastPosition = Vector3.zero;
        _lastSentMapIcon = false;
    }

    /// <summary>
    /// Get a valid instance of the GameMap class.
    /// </summary>
    /// <returns>An instance of GameMap.</returns>
    private GameMap GetGameMap() {
        var gameManager = global::GameManager.instance;
        if (gameManager == null) {
            return null;
        }

        var gameMapObject = gameManager.gameMap;
        if (gameMapObject == null) {
            return null;
        }

        var gameMap = gameMapObject.GetComponent<GameMap>();
        if (gameMap == null) {
            return null;
        }

        return gameMap;
    }

    /// <summary>
    /// Get an area object by its name.
    /// </summary>
    /// <param name="gameMap">The GameMap instance.</param>
    /// <param name="name">The name of the area to retrieve.</param>
    /// <returns>A GameObject representing the map area.</returns>
    private static GameObject GetAreaObjectByName(GameMap gameMap, string name) {
        switch (name) {
            case "ABYSS":
                return gameMap.areaAncientBasin;
            case "CITY":
            case "KINGS_STATION":
            case "SOUL_SOCIETY":
            case "LURIENS_TOWER":
                return gameMap.areaCity;
            case "CLIFFS":
                return gameMap.areaCliffs;
            case "CROSSROADS":
            case "SHAMAN_TEMPLE":
                return gameMap.areaCrossroads;
            case "MINES":
                return gameMap.areaCrystalPeak;
            case "DEEPNEST":
            case "BEASTS_DEN":
                return gameMap.areaDeepnest;
            case "FOG_CANYON":
            case "MONOMON_ARCHIVE":
                return gameMap.areaFogCanyon;
            case "WASTES":
            case "QUEENS_STATION":
                return gameMap.areaFungalWastes;
            case "GREEN_PATH":
                return gameMap.areaGreenpath;
            case "OUTSKIRTS":
            case "HIVE":
            case "COLOSSEUM":
                return gameMap.areaKingdomsEdge;
            case "ROYAL_GARDENS":
                return gameMap.areaQueensGardens;
            case "RESTING_GROUNDS":
                return gameMap.areaRestingGrounds;
            case "TOWN":
            case "KINGS_PASS":
                return gameMap.areaDirtmouth;
            case "WATERWAYS":
            case "GODSEEKER_WASTE":
                return gameMap.areaWaterways;
            default:
                return gameMap.gameObject.FindGameObjectInChildren(name);
        }
    }

    /// <inheritdoc />
    public bool TryGetEntry(ushort id, out IPlayerMapEntry playerMapEntry) {
        var found = _mapEntries.TryGetValue(id, out var entry);
        playerMapEntry = entry;

        return found;
    }

    /// <summary>
    /// An entry for an icon of a player.
    /// </summary>
    private class PlayerMapEntry : IPlayerMapEntry {
        /// <inheritdoc />
        public bool HasMapIcon { get; set; }

        /// <inheritdoc />
        public Vector2 Position { get; set; } = Vector2.Zero;

        /// <summary>
        /// The game object corresponding to the map icon.
        /// </summary>
        public GameObject GameObject { get; set; }
    }
}
