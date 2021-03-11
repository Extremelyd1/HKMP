using System.Collections.Generic;
using HKMP.Fsm;
using HKMP.Game;
using HKMP.Networking;
using HKMP.Networking.Client;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HKMP.Animation {
    public class DreamShieldManager {

        private readonly NetClient _netClient;
        private readonly PlayerManager _playerManager;

        private readonly Dictionary<int, GameObject> _dreamshields;

        private GameObject _dreamshieldPrefab;
        private bool _isPrefabCreated;

        private bool _hasDreamshieldActive;

        public DreamShieldManager(NetworkManager networkManager, PlayerManager playerManager, PacketManager packetManager) {
            _netClient = networkManager.GetNetClient();
            _playerManager = playerManager;

            // Create a new dictionary to store the Dreamshield objects per ID
            _dreamshields = new Dictionary<int, GameObject>();

            // Register when the HeroController starts, so we can register the spawn event of the Dream Shield
            On.HeroController.Start += HeroControllerOnStart;

            // Register relevant packet handlers for Dreamshield related packets
            packetManager.RegisterClientPacketHandler<ClientDreamshieldSpawnPacket>(PacketId.DreamshieldSpawn, OnDreamshieldSpawn);
            packetManager.RegisterClientPacketHandler<ClientDreamshieldDespawnPacket>(PacketId.DreamshieldDespawn, OnDreamshieldDespawn);
        }

        private void CreateDreamshieldPrefab() {
            // Obtain the Charm Effects-Spawn Orbit Shield FSM from the HeroController
            var orbitShieldFsm = HeroController.instance.fsm_orbitShield;
            // Get the spawn action where the orbit shield prefab is stored
            var spawnAction = orbitShieldFsm.GetAction<SpawnObjectFromGlobalPool>("Spawn", 2);

            // Instantiate our own prefab at zero position and default rotation
            _dreamshieldPrefab = Object.Instantiate(
                spawnAction.gameObject.Value,
                Vector3.zero,
                Quaternion.identity
            );
            // Untag it, otherwise the FSM will interfere with it
            _dreamshieldPrefab.tag = "Untagged";
            // We don't want to have it active until we spawn a new one from the prefab
            _dreamshieldPrefab.SetActive(false);
            // And we want to use it in multiple scenes, so it shouldn't be destroyed
            Object.DontDestroyOnLoad(_dreamshieldPrefab);

            // Destroy the attached FSMs in the root object
            Object.Destroy(_dreamshieldPrefab.LocateMyFSM("Control"));
            Object.Destroy(_dreamshieldPrefab.LocateMyFSM("Focus Speedup"));

            // Destroy the FSMs in the child Shield object
            var shieldObject = _dreamshieldPrefab.FindGameObjectInChildren("Shield");
            Object.Destroy(shieldObject.LocateMyFSM("Shield Hit"));
            Object.Destroy(shieldObject.LocateMyFSM("Blocker Effect"));

            // Add our own Rotate and FollowObject components, which will
            // respectively rotate the shield and follow the correct player
            _dreamshieldPrefab.AddComponent<Fsm.Rotate>();
            _dreamshieldPrefab.AddComponent<FollowObject>();
        }
        
        private void OnDreamshieldSpawn(ClientDreamshieldSpawnPacket packet) {
            if (_dreamshields.ContainsKey(packet.Id)) {
                Logger.Info(this, $"Tried to spawn a dreamshield for ID {packet.Id}, but there was already one");
                return;
            }
            
            SpawnDreamshield(packet.Id);
        }
        
        private void OnDreamshieldDespawn(ClientDreamshieldDespawnPacket packet) {
            DespawnDreamshield(packet.Id);
        }

        private void SpawnDreamshield(int id) {
            var playerObject = _playerManager.GetPlayerObject(id);
            if (playerObject == null) {
                return;
            }

            var dreamshield = _dreamshieldPrefab.Spawn();

            // Set the speed for the rotation, this is the default
            dreamshield.GetComponent<Fsm.Rotate>().SetAngles(0, 0, 110f);

            // Set the object to follow and the offset for the shield
            // The offset is based on the FSM
            var followObject = dreamshield.GetComponent<FollowObject>();
            followObject.GameObject = playerObject;
            followObject.Offset = new Vector3(0, -0.5f, 0);
            
            // Activate it and add it to the mapping
            dreamshield.SetActive(true);
            _dreamshields.Add(id, dreamshield);
        }

        public void DespawnDreamshield(int id) {
            if (!_dreamshields.ContainsKey(id)) {
                return;
            }

            var dreamshield = _dreamshields[id];

            // The child object Shield contains the sprite animator for disappearing
            var shieldObject = dreamshield.FindGameObjectInChildren("Shield");

            // Get the animator and play the Disappear clip
            var spriteAnimator = shieldObject.GetComponent<tk2dSpriteAnimator>();
            spriteAnimator.Play("Disappear");
            
            // Disable the collider in the meantime
            shieldObject.GetComponent<BoxCollider2D>().enabled = false;
            
            // Destroy it after the clip has finished playing
            Object.Destroy(dreamshield, spriteAnimator.GetClipByName("Disappear").Duration);
            
            // Remove it from our mapping
            _dreamshields.Remove(id);
        }

        private void HeroControllerOnStart(On.HeroController.orig_Start orig, HeroController self) {
            // Execute original method
            orig(self);
            
            RegisterDreamShieldSpawns();

            // Create the prefab if we haven't already, we can only do this once the local player is initialized
            if (!_isPrefabCreated) {
                CreateDreamshieldPrefab();
                _isPrefabCreated = true;
            }
        }

        private void RegisterDreamShieldSpawns() {
            // Get the FSM from the HeroController
            var orbitShieldFsm = HeroController.instance.fsm_orbitShield;

            // Insert a method after the spawn action, since we know that the Dreamshield has spawned then
            orbitShieldFsm.InsertMethod("Spawn", 3, () => {
                _hasDreamshieldActive = true;
                
                // Find the Control FSM and insert a method in the Disappear state
                var localDreamshield = GameObject.FindWithTag("Orbit Shield");
                var shieldControlFsm = localDreamshield.LocateMyFSM("Control");
                shieldControlFsm.InsertMethod("Disappear", 2, () => {
                    _hasDreamshieldActive = false;

                    // Only send a packet if we are connected
                    if (_netClient.IsConnected) {
                        Logger.Info(this, "Dreamshield despawned, sending despawn packet");
                    
                        _netClient.SendUdp(new ServerDreamshieldDespawnPacket().CreatePacket());
                    }
                });

                // Only send a packet if we are connected
                if (!_netClient.IsConnected) {
                    return;
                }

                Logger.Info(this, "Dreamshield spawned, sending spawn packet");

                _netClient.SendUdp(new ServerDreamshieldSpawnPacket().CreatePacket());
            });
        }
    }
}