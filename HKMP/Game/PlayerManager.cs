using System.Collections.Generic;
using HKMP.Networking;
using ModCommon;
using TMPro;
using UnityEngine;

namespace HKMP.Game {
    /**
     * Class that manages player objects, spawning and destroying thereof.
     */
    public class PlayerManager {
        private readonly Game.Settings.GameSettings _gameSettings;
        
        private readonly Dictionary<int, GameObject> _playerObjects;

        private readonly GameObject _playerPrefab;
        
        public PlayerManager(NetworkManager networkManager, Game.Settings.GameSettings gameSettings, ModSettings settings) {
            _gameSettings = gameSettings;
            
            _playerObjects = new Dictionary<int, GameObject>();
            
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
                typeof(tk2dSpriteAnimator)
            ) {
                layer = 9
            };

            // Add some extra gameObject related to animation effects
            new GameObject("Attacks") {layer = 9}.transform.SetParent(_playerPrefab.transform);
            new GameObject("Effects") {layer = 9}.transform.SetParent(_playerPrefab.transform);
            new GameObject("Spells")  {layer = 9}.transform.SetParent(_playerPrefab.transform);
            
            _playerPrefab.SetActive(false);
            Object.DontDestroyOnLoad(_playerPrefab);
            
            // Register the Hero Controller Start, which is when the local player spawns
            On.HeroController.Start += (orig, self) => {
                // Execute the original method
                orig(self);
                // If we are connect to a server, add a username to the player object
                if (networkManager.GetNetClient().IsConnected) {
                    AddNameToPlayerObject(HeroController.instance.gameObject, settings.Username);
                }
            };
            networkManager.GetNetClient().RegisterOnConnect(() => {
                // We should only be able to connect during a gameplay scene,
                // which is when the player is spawned already, so we can add the username
                // TODO: make a settings for this
                AddNameToPlayerObject(HeroController.instance.gameObject, settings.Username);
            });
        }

        public void UpdatePosition(int id, Vector3 position) {
            if (!_playerObjects.ContainsKey(id)) {
                // TODO: maybe suppress this message, this might occur often if the EnterScene packet is late
                Logger.Warn(this, $"Tried to update position for ID {id} while object did not exists");
                return;
            }

            var playerObject = _playerObjects[id];
            playerObject.transform.position = position;
        }

        public void UpdateScale(int id, Vector3 scale) {
            if (!_playerObjects.ContainsKey(id)) {
                // TODO: maybe suppress this message, this might occur often if the EnterScene packet is late
                Logger.Warn(this, $"Tried to update scale for ID {id} while object did not exists");
                return;
            }

            var playerObject = _playerObjects[id];
            playerObject.transform.localScale = scale;
        }

        public GameObject GetPlayerObject(int id) {
            if (!_playerObjects.ContainsKey(id)) {
                Logger.Error(this, $"Tried to get the player object that does not exists for ID {id}");
                return null;
            }

            return _playerObjects[id];
        }

        // TODO: investigate whether it is better to disable/setActive(false) player objects instead of destroying
        // and only destroy when player left server
        public void DestroyPlayer(int id) {
            if (!_playerObjects.ContainsKey(id)) {
                Logger.Warn(this, $"Tried to destroy player that does not exists for ID {id}");
                return;
            }
            
            // Destroy gameObject and remove from mapping
            Object.Destroy(_playerObjects[id]);
            _playerObjects.Remove(id);
        }

        public void DestroyAllPlayers() {
            foreach (var playerObject in _playerObjects.Values) {
                // Destroy gameObject
                Object.Destroy(playerObject);
            }
            
            // Clear mapping
            _playerObjects.Clear();
        }
        
        public void SpawnPlayer(int id, string name) {
            if (_playerObjects.ContainsKey(id)) {
                Logger.Warn(this, $"We already have created a player object for ID {id}");
                return;
            }
            
            // Instantiate the player object from the prefab
            var playerObject = Object.Instantiate(_playerPrefab);
            Object.DontDestroyOnLoad(playerObject);
            
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
            var anim = playerObject.GetComponent<tk2dSpriteAnimator>();
            anim.Library = localPlayerObject.GetComponent<tk2dSpriteAnimator>().Library;

            AddNameToPlayerObject(playerObject, name);

            // Store the player object in the mapping
            _playerObjects[id] = playerObject;
        }

        private void AddNameToPlayerObject(GameObject playerObject, string name) {
            // Create a name object to set the username to, slightly above the player object
            var nameObject = Object.Instantiate(
                new GameObject("Username"),
                playerObject.transform.position + new Vector3(0, 1.25f, 0),
                Quaternion.identity
            );
            nameObject.name = "Username";
            nameObject.transform.SetParent(playerObject.transform);
            nameObject.transform.localScale = new Vector3(0.25f, 0.25f, nameObject.transform.localScale.z);
            
            // Add a TextMeshPro component to it, so we can render text
            var textMeshObject = nameObject.AddComponent<TextMeshPro>();
            textMeshObject.text = name;
            textMeshObject.alignment = TextAlignmentOptions.Center;
            textMeshObject.fontSize = 24;
            textMeshObject.outlineColor = Color.black;
            textMeshObject.outlineWidth = 0.1f;
            // Add a component to it to make sure that the text does not get flipped when the player turns around
            nameObject.AddComponent<KeepWorldScalePositive>();
        }

        public void RemoveNameFromLocalPlayer() {
            RemoveNameFromPlayerObject(HeroController.instance.gameObject);
        }

        private void RemoveNameFromPlayerObject(GameObject playerObject) {
            // Get the name object
            var nameObject = playerObject.FindGameObjectInChildren("Username");

            // Destroy it if it exists
            if (nameObject != null) {
                Object.Destroy(nameObject);
            }
        }

        public void OnGameSettingsUpdated() {
            // Loop over all player objects
            foreach (var playerObject in _playerObjects.Values) {
                // Enable the DamageHero component based on whether both PvP and body damage are enabled
                // And move the object to the correct layer
                if (_gameSettings.IsPvpEnabled && _gameSettings.IsBodyDamageEnabled) {
                    playerObject.layer = 11;
                    playerObject.GetComponent<DamageHero>().enabled = true;
                } else {
                    playerObject.layer = 9;
                    playerObject.GetComponent<DamageHero>().enabled = false;
                }
            }
        }

    }
}