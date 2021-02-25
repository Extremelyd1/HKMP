using System.Collections.Generic;
using UnityEngine;

namespace HKMP.Game {
    public class PlayerManager {

        private readonly Dictionary<int, GameObject> _playerObjects;

        private readonly GameObject _playerPrefab;
        
        public PlayerManager() {
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
            _playerPrefab.SetActive(false);
            Object.DontDestroyOnLoad(_playerPrefab);
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
        
        public void SpawnPlayer(int id) {
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
            playerObject.GetComponent<DamageHero>().enabled = false;
            
            // Copy over mesh filter variables
            var meshFilter = playerObject.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.mesh;
            Mesh localMesh = localPlayerObject.GetComponent<MeshFilter>().sharedMesh;
            
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
            
            // playerObject.GetComponent<tk2dSpriteAnimator>().Play();
            
            // Store the player object in the mapping
            _playerObjects[id] = playerObject;
        }

    }
}