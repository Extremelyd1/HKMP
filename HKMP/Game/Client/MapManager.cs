using Hkmp.Concurrency;
using Hkmp.Networking.Client;
using Modding;
using UnityEngine;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client {
    /// <summary>
    /// A class that manages player locations on the in-game map.
    /// </summary>
    internal class MapManager {
        /// <summary>
        /// The net client instance.
        /// </summary>
        private readonly NetClient _netClient;
        /// <summary>
        /// The current game settings.
        /// </summary>
        private readonly Settings.GameSettings _gameSettings;

        // TODO: this shouldn't have to be a concurrent dictionary, since all client-side actions are performed on
        // the main thread of Unity right?
        /// <summary>
        /// Dictionary containing map icon objects per player ID
        /// </summary>
        private readonly ConcurrentDictionary<int, GameObject> _mapIcons;

        /// <summary>
        /// The last sent map position.
        /// </summary>
        private Vector3 _lastPosition;

        /// <summary>
        /// Whether the last update we sent was an empty one.
        /// </summary>
        private bool _lastSentEmptyUpdate;

        /// <summary>
        /// Whether we should display the map icons. True if the map is opened, false otherwise.
        /// </summary>
        private bool _displayingIcons;

        public MapManager(NetClient netClient, Settings.GameSettings gameSettings) {
            _netClient = netClient;
            _gameSettings = gameSettings;

            _mapIcons = new ConcurrentDictionary<int, GameObject>();

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

            var sendEmptyUpdate = false;

            if (!_gameSettings.AlwaysShowMapIcons) {
                if (!_gameSettings.OnlyBroadcastMapIconWithWaywardCompass) {
                    sendEmptyUpdate = true;
                } else {
                    // We do not always show map icons, but only when we are wearing wayward compass
                    // So we need to check whether we are wearing wayward compass
                    if (!PlayerData.instance.GetBool(nameof(PlayerData.equippedCharm_2))) {
                        sendEmptyUpdate = true;
                    }
                }
            }

            if (sendEmptyUpdate) {
                if (_lastSentEmptyUpdate) {
                    return;
                }

                _netClient.UpdateManager.UpdatePlayerMapPosition(Vector2.Zero);

                // Set the last position to zero, so that when we
                // equip it again, we immediately send the update since the position changed
                _lastPosition = Vector3.zero;

                _lastSentEmptyUpdate = true;

                return;
            }

            var newPosition = GetMapLocation();

            // Only send update if the position changed
            if (newPosition != _lastPosition) {
                var vec2 = new Vector2(newPosition.x, newPosition.y);

                _netClient.UpdateManager.UpdatePlayerMapPosition(vec2);

                // Update the last position, since it changed
                _lastPosition = newPosition;

                _lastSentEmptyUpdate = false;
            }
        }

        /// <summary>
        /// Get the current map location of the local player.
        /// </summary>
        /// <returns>A Vector3 representing the map location.</returns>
        private Vector3 GetMapLocation() {
            // Get the game manager instance
            var gameManager = global::GameManager.instance;
            // Get the current map zone of the game manager and check whether we are in
            // an area that doesn't shop up on the map
            var currentMapZone = gameManager.GetCurrentMapZone();
            if (currentMapZone.Equals("DREAM_WORLD")
                || currentMapZone.Equals("WHITE_PALACE")
                || currentMapZone.Equals("GODS_GLORY")) {
                return Vector3.zero;
            }

            // Get the game map instance
            var gameMap = GetGameMap();
            if (gameMap == null) {
                return Vector3.zero;
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
                return Vector3.zero;
            }

            for (var i = 0; i < areaObject.transform.childCount; i++) {
                var childObject = areaObject.transform.GetChild(i).gameObject;
                if (childObject.name.Equals(sceneName)) {
                    sceneObject = childObject;
                    break;
                }
            }

            if (sceneObject == null) {
                return Vector3.zero;
            }

            var sceneObjectPos = sceneObject.transform.localPosition;
            var areaObjectPos = areaObject.transform.localPosition;

            var currentScenePos = new Vector3(
                sceneObjectPos.x + areaObjectPos.x,
                sceneObjectPos.y + areaObjectPos.y,
                0f
            );

            var size = sceneObject.GetComponent<SpriteRenderer>().sprite.bounds.size;
            var gameMapScale = gameMap.transform.localScale;

            Vector3 position;

            if (gameMap.inRoom) {
                position = new Vector3(
                    currentScenePos.x - size.x / 2.0f + (gameMap.doorX + gameMap.doorOriginOffsetX) /
                    gameMap.doorSceneWidth *
                    size.x,
                    currentScenePos.y - size.y / 2.0f + (gameMap.doorY + gameMap.doorOriginOffsetY) /
                    gameMap.doorSceneHeight *
                    gameMapScale.y,
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

            return position;
        }

        /// <summary>
        /// Callback method for when we receive a map update from another player.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <param name="position">The new position on the map.</param>
        public void OnPlayerMapUpdate(ushort id, Vector2 position) {
            if (position == Vector2.Zero) {
                // We have received an empty update, which means that we need to remove
                // the icon if it exists
                if (_mapIcons.TryGetValue(id, out _)) {
                    RemovePlayerIcon(id);
                }

                return;
            }

            // If there does not exist a player icon for this id yet, we create it
            if (!_mapIcons.TryGetValue(id, out _)) {
                CreatePlayerIcon(id, position);

                return;
            }

            // Check whether the object still exists
            var mapObject = _mapIcons[id];
            if (mapObject == null) {
                _mapIcons.Remove(id);
                return;
            }

            // Check if the transform is still valid and otherwise destroy the object
            // This is possible since whenever we receive a new update packet, we
            // will just create a new map icon
            var transform = mapObject.transform;
            if (transform == null) {
                Object.Destroy(mapObject);
                _mapIcons.Remove(id);
                return;
            }
            
            // Subtract ID * 0.01 from the Z position to prevent Z-fighting with the icons
            var unityPosition = new Vector3(
                position.X, 
                position.Y,
                id * -0.01f
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
        /// Update all existing map icons based on whether they should be active according to game settings.
        /// </summary>
        private void UpdateMapIconsActive() {
            foreach (var mapIcon in _mapIcons.GetCopy().Values) {
                mapIcon.SetActive(_displayingIcons);
            }
        }

        /// <summary>
        /// Create a map icon for a player.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        /// <param name="position">The position of the map icon.</param>
        private void CreatePlayerIcon(ushort id, Vector2 position) {
            var gameMap = GetGameMap();
            if (gameMap == null) {
                return;
            }

            var compassIconPrefab = gameMap.compassIcon;
            if (compassIconPrefab == null) {
                Logger.Get().Error(this, "CompassIcon prefab is null");
                return;
            }

            // Create a new player icon relative to the game map
            var mapIcon = Object.Instantiate(
                compassIconPrefab,
                gameMap.gameObject.transform
            );
            mapIcon.SetActive(_displayingIcons);

            // Subtract ID * 0.01 from the Z position to prevent Z-fighting with the icons
            var unityPosition = new Vector3(
                position.X, 
                position.Y,
                id * -0.01f
            );

            // Set the position of the player icon
            mapIcon.transform.localPosition = unityPosition;

            // Remove the bob effect when walking with the map
            Object.Destroy(mapIcon.LocateMyFSM("Mapwalk Bob"));

            // Put it in the list
            _mapIcons[id] = mapIcon;
        }

        /// <summary>
        /// Remove the map icon for a player.
        /// </summary>
        /// <param name="id">The ID of the player.</param>
        public void RemovePlayerIcon(ushort id) {
            if (!_mapIcons.TryGetValue(id, out var playerIcon)) {
                Logger.Get().Warn(this, $"Tried to remove player icon of ID: {id}, but it didn't exist");
                return;
            }

            // Destroy the player icon and then remove it from the list
            Object.Destroy(playerIcon);
            _mapIcons.Remove(id);
        }

        /// <summary>
        /// Remove all map icons.
        /// </summary>
        public void RemoveAllIcons() {
            // Destroy all existing map icons
            foreach (var mapIcon in _mapIcons.GetCopy().Values) {
                Object.Destroy(mapIcon);
            }

            // Clear the mapping
            _mapIcons.Clear();
        }

        /// <summary>
        /// Callback method for when the local user disconnects.
        /// </summary>
        private void OnDisconnect() {
            RemoveAllIcons();

            // Reset variables to their initial values
            _lastPosition = Vector3.zero;
            _lastSentEmptyUpdate = false;
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
                    return null;
            }
        }
    }
}
