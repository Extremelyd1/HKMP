// using System.Collections;
// using System.Collections.Generic;
// using HKMP.Fsm;
// using HKMP.Game;
// using HKMP.Game.Client;
// using HKMP.Networking;
// using HKMP.Networking.Client;
// using HKMP.Networking.Packet;
// using HKMP.Networking.Packet.Data;
// using HKMP.Util;
// using HutongGames.PlayMaker.Actions;
// using ModCommon;
// using ModCommon.Util;
// using UnityEngine;
// using Object = UnityEngine.Object;
//
// namespace HKMP.Animation {
//     // TODO: add slash effect to Dreamshield
//     // TODO: add DamageHero so it can damage other players in PvP
//     // TODO: sync the shield rotation with the update packet?
//     // TODO: if player has shield active before connecting, the shield will not spawn
//     // TODO: pausing game will desync shield, disable pausing while connected to MP game?
//     // TODO: stress-test this for bugs in interactions with other components
//     public class DreamShieldManager {
//
//         private readonly NetClient _netClient;
//         private readonly PlayerManager _playerManager;
//
//         private readonly Dictionary<ushort, GameObject> _dreamshields;
//
//         private GameObject _dreamshieldPrefab;
//         private bool _isPrefabCreated;
//         
//         // TODO: remove warning suppression once this is used
// #pragma warning disable 414
//         private bool _hasDreamshieldActive;
// #pragma warning restore 414
//
//         private bool _registeredDreamshieldEvents;
//
//         private AudioClip _blockAudioClip;
//         private AudioClip _reformAudioClip;
//
//         public DreamShieldManager(NetworkManager networkManager, PlayerManager playerManager, PacketManager packetManager) {
//             _netClient = networkManager.GetNetClient();
//             _playerManager = playerManager;
//
//             // Create a new dictionary to store the Dreamshield objects per ID
//             _dreamshields = new Dictionary<ushort, GameObject>();
//
//             // Register when the HeroController starts, so we can register the spawn event of the Dream Shield
//             On.HeroController.Start += HeroControllerOnStart;
//
//             // Register relevant packet handlers for Dreamshield related packets
//             // packetManager.RegisterClientPacketHandler<ClientDreamshieldSpawnPacket>(PacketId.DreamshieldSpawn, OnDreamshieldSpawn);
//             // packetManager.RegisterClientPacketHandler<ClientDreamshieldDespawnPacket>(PacketId.DreamshieldDespawn, OnDreamshieldDespawn);
//             // packetManager.RegisterClientPacketHandler<ClientDreamshieldUpdatePacket>(PacketId.DreamshieldUpdate, OnDreamshieldUpdate);
//         }
//
//         private void CreateDreamshieldPrefab() {
//             // Obtain the Charm Effects-Spawn Orbit Shield FSM from the HeroController
//             var orbitShieldFsm = HeroController.instance.fsm_orbitShield;
//             // Get the spawn action where the orbit shield prefab is stored
//             var spawnAction = orbitShieldFsm.GetAction<SpawnObjectFromGlobalPool>("Spawn", 2);
//
//             // Instantiate our own prefab at zero position and default rotation
//             _dreamshieldPrefab = Object.Instantiate(
//                 spawnAction.gameObject.Value,
//                 Vector3.zero,
//                 Quaternion.identity
//             );
//             // Untag it, otherwise the FSM will interfere with it
//             _dreamshieldPrefab.tag = "Untagged";
//             // We don't want to have it active until we spawn a new one from the prefab
//             _dreamshieldPrefab.SetActive(false);
//             // And we want to use it in multiple scenes, so it shouldn't be destroyed
//             Object.DontDestroyOnLoad(_dreamshieldPrefab);
//
//             // Destroy the attached FSMs in the root object
//             Object.Destroy(_dreamshieldPrefab.LocateMyFSM("Control"));
//             Object.Destroy(_dreamshieldPrefab.LocateMyFSM("Focus Speedup"));
//
//             // Destroy the FSMs in the child Shield object
//             var shieldObject = _dreamshieldPrefab.FindGameObjectInChildren("Shield");
//             Object.Destroy(shieldObject.LocateMyFSM("Shield Hit"));
//             Object.Destroy(shieldObject.LocateMyFSM("Blocker Effect"));
//             
//             // Add our own Rotate and FollowObject components, which will
//             // respectively rotate the shield and follow the correct player
//             _dreamshieldPrefab.AddComponent<Fsm.Rotate>();
//             _dreamshieldPrefab.AddComponent<FollowObject>();
//         }
//         
//         private void OnDreamshieldSpawn(ClientDreamshieldSpawnPacket packet) {
//             if (_dreamshields.ContainsKey(packet.Id)) {
//                 Logger.Get().Info(this, $"Tried to spawn a dreamshield for ID {packet.Id}, but there was already one");
//                 return;
//             }
//             
//             SpawnDreamshield(packet.Id);
//         }
//         
//         private void OnDreamshieldDespawn(ClientDreamshieldDespawnPacket packet) {
//             DespawnDreamshield(packet.Id);
//         }
//         
//         private void OnDreamshieldUpdate(ClientDreamshieldUpdatePacket packet) {
//             if (!_dreamshields.ContainsKey(packet.Id)) {
//                 return;
//             }
//
//             if (packet.BlockEffect) {
//                 OnDreamshieldBlock(packet.Id);
//             } else if (packet.BreakEffect) {
//                 OnDreamshieldBreak(packet.Id);
//             } else if (packet.ReformEffect) {
//                 MonoBehaviourUtil.Instance.StartCoroutine(
//                     OnDreamshieldReform(packet.Id)
//                 );
//             }
//         }
//
//         private void SpawnDreamshield(ushort id) {
//             var playerContainer = _playerManager.GetPlayerContainer(id);
//             if (playerContainer == null) {
//                 return;
//             }
//
//             var dreamshield = Object.Instantiate(
//                 _dreamshieldPrefab,
//                 playerContainer.transform
//             );
//
//             // Set the speed for the rotation, this is the default
//             dreamshield.GetComponent<Fsm.Rotate>().SetAngles(0, 0, 110f);
//
//             // Set the object to follow and the offset for the shield
//             // The offset is based on the FSM
//             var followObject = dreamshield.GetComponent<FollowObject>();
//             followObject.GameObject = playerContainer;
//             followObject.Offset = new Vector3(0, -0.5f, 0);
//             
//             // Activate it and add it to the mapping
//             dreamshield.SetActive(true);
//             _dreamshields.Add(id, dreamshield);
//         }
//
//         public void DespawnDreamshield(ushort id) {
//             if (!_dreamshields.ContainsKey(id)) {
//                 return;
//             }
//
//             var dreamshield = _dreamshields[id];
//
//             // The child object Shield contains the sprite animator for disappearing
//             var shieldObject = dreamshield.FindGameObjectInChildren("Shield");
//
//             // Get the animator and play the Disappear clip
//             var spriteAnimator = shieldObject.GetComponent<tk2dSpriteAnimator>();
//             spriteAnimator.Play("Disappear");
//             
//             // Disable the collider in the meantime
//             shieldObject.GetComponent<BoxCollider2D>().enabled = false;
//             
//             // Destroy it after the clip has finished playing
//             Object.Destroy(dreamshield, spriteAnimator.GetClipByName("Disappear").Duration);
//             
//             // Remove it from our mapping
//             _dreamshields.Remove(id);
//         }
//
//         private void HeroControllerOnStart(On.HeroController.orig_Start orig, HeroController self) {
//             // Execute original method
//             orig(self);
//             
//             RegisterDreamShieldEvents();
//
//             // Create the prefab if we haven't already, we can only do this once the local player is initialized
//             if (!_isPrefabCreated) {
//                 CreateDreamshieldPrefab();
//                 _isPrefabCreated = true;
//             }
//         }
//
//         private void RegisterDreamShieldEvents() {
//             // Get the FSM from the HeroController
//             var orbitShieldFsm = HeroController.instance.fsm_orbitShield;
//
//             // Insert a method after the spawn action, since we know that the Dreamshield has spawned then
//             orbitShieldFsm.InsertMethod("Spawn", 3, () => {
//                 OnLocalDreamshieldSpawn();
//
//                 // Make sure to only register the rest of the events once and not
//                 // every time a Dreamshield spawns
//                 if (_registeredDreamshieldEvents) {
//                     return;
//                 }
//                 _registeredDreamshieldEvents = true;
//                 
//                 // Find the Control FSM and insert a method in the Disappear state
//                 var localDreamshield = GameObject.FindWithTag("Orbit Shield");
//                 var shieldControlFsm = localDreamshield.LocateMyFSM("Control");
//                 shieldControlFsm.InsertMethod("Disappear", 2, OnLocalDreamshieldDespawn);
//
//                 var shieldObject = localDreamshield.FindGameObjectInChildren("Shield");
//                 var shieldHitFsm = shieldObject.LocateMyFSM("Shield Hit");
//                 
//                 shieldHitFsm.InsertMethod("Block Effect", 2, OnLocalDreamshieldBlock);
//                 shieldHitFsm.InsertMethod("Break", 6, OnLocalDreamshieldBreak);
//                 shieldHitFsm.InsertMethod("Reform", 4, OnLocalDreamshieldReform);
//
//                 var reformAudioPlayAction = shieldHitFsm.GetAction<AudioPlayerOneShotSingle>("Reform", 3);
//                 _reformAudioClip = (AudioClip) reformAudioPlayAction.audioClip.Value;
//
//                 var blockEffectFsm = shieldObject.LocateMyFSM("Block Effect");
//                 var blockAudioPlayAction = blockEffectFsm.GetAction<AudioPlaySimple>("Block", 1);
//                 _blockAudioClip = (AudioClip) blockAudioPlayAction.oneShotClip.Value;
//             });
//         }
//
//         private void OnLocalDreamshieldSpawn() {
//             _hasDreamshieldActive = true;
//             
//             // Only send a packet if we are connected
//             if (!_netClient.IsConnected) {
//                 return;
//             }
//
//             Logger.Get().Info(this, "Dreamshield spawned, sending spawn packet");
//
//             // _netClient.SendUdp(new ServerDreamshieldSpawnPacket().CreatePacket());
//         }
//
//         private void OnLocalDreamshieldDespawn() {
//             _hasDreamshieldActive = false;
//
//             // Only send a packet if we are connected
//             if (!_netClient.IsConnected) {
//                 return;
//             }
//
//             Logger.Get().Info(this, "Dreamshield despawned, sending despawn packet");
//                     
//             // _netClient.SendUdp(new ServerDreamshieldDespawnPacket().CreatePacket());
//         }
//
//         private void OnLocalDreamshieldBlock() {
//             // Only send a packet if we are connected
//             if (!_netClient.IsConnected) {
//                 return;
//             }
//
//             Logger.Get().Info(this, "Dreamshield blocked, sending update packet");
//             
//             var dreamshieldUpdatePacket = new ServerDreamshieldUpdatePacket {
//                 BlockEffect = true,
//                 BreakEffect = false,
//                 ReformEffect = false
//             };
//             // _netClient.SendUdp(dreamshieldUpdatePacket.CreatePacket());
//         }
//
//         private void OnDreamshieldBlock(ushort id) {
//             var dreamshield = _dreamshields[id];
//
//             var shieldObject = dreamshield.FindGameObjectInChildren("Shield");
//             shieldObject.GetComponent<tk2dSpriteAnimator>().Play("Block");
//
//             shieldObject.GetComponent<AudioSource>().PlayOneShot(_blockAudioClip);
//         }
//
//         private void OnLocalDreamshieldBreak() {
//             // Only send a packet if we are connected
//             if (!_netClient.IsConnected) {
//                 return;
//             }
//
//             Logger.Get().Info(this, "Dreamshield broke, sending update packet");
//             
//             var dreamshieldUpdatePacket = new ServerDreamshieldUpdatePacket {
//                 BlockEffect = false,
//                 BreakEffect = true,
//                 ReformEffect = false
//             };
//             // _netClient.SendUdp(dreamshieldUpdatePacket.CreatePacket());
//         }
//
//         private void OnDreamshieldBreak(ushort id) {
//             var dreamshield = _dreamshields[id];
//
//             dreamshield.FindGameObjectInChildren("Laser Stopper").SetActive(false);
//
//             var shieldObject = dreamshield.FindGameObjectInChildren("Shield");
//             
//             shieldObject.GetComponent<BoxCollider2D>().enabled = false;
//             shieldObject.GetComponent<tk2dSpriteAnimator>().Play("Enemy Hit");
//         }
//         
//         private void OnLocalDreamshieldReform() {
//             // Only send a packet if we are connected
//             if (!_netClient.IsConnected) {
//                 return;
//             }
//
//             Logger.Info("Dreamshield reformed, sending update packet");
//             
//             var dreamshieldUpdatePacket = new ServerDreamshieldUpdatePacket {
//                 BlockEffect = false,
//                 BreakEffect = false,
//                 ReformEffect = true
//             };
//             // _netClient.SendUdp(dreamshieldUpdatePacket.CreatePacket());
//         }
//
//         private IEnumerator OnDreamshieldReform(ushort id) {
//             var dreamshield = _dreamshields[id];
//             
//             var shieldObject = dreamshield.FindGameObjectInChildren("Shield");
//
//             var spriteAnimator = shieldObject.GetComponent<tk2dSpriteAnimator>();
//             spriteAnimator.Play("Reform");
//             
//             shieldObject.GetComponent<AudioSource>().PlayOneShot(_reformAudioClip);
//
//             yield return new WaitForSeconds(spriteAnimator.GetClipByName("Reform").Duration);
//
//             shieldObject.GetComponent<BoxCollider2D>().enabled = true;
//             dreamshield.FindGameObjectInChildren("Laser Stopper").SetActive(true);
//             
//             spriteAnimator.Play("Idle");
//         }
//     }
// }



