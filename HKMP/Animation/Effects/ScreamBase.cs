using System.Collections;
using System.Collections.Generic;
using HKMP.Networking.Packet.Custom;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public abstract class ScreamBase : IAnimationEffect {
        public abstract void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet);

        public IEnumerator Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet, string screamClipName, string screamObjectName) {
            // A convoluted way of getting to an AudioSource so we can play the clip for this effect
            // I tried getting it from the AudioPlay object, but that one is always null for some reason
            // TODO: find a way to clean this up
            var spellControl = HeroController.instance.spellControl;
            var fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 1", 3).gameObject.Value;
            var fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            var audioAction = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 6);
            var audioPlayerObj = audioAction.audioPlayer.Value;
            var audioPlayer = audioPlayerObj.Spawn(playerObject.transform);
            var audioSource = audioPlayer.GetComponent<AudioSource>();

            // Get the correct scream audio clip based on the given parameter
            var screamClip = (AudioClip) spellControl.GetAction<AudioPlay>(screamClipName, 1).oneShotClip.Value;
            // And play it
            audioSource.PlayOneShot(screamClip);
            
            var localPlayerSpells = spellControl.gameObject;
            var playerSpells = playerObject.FindGameObjectInChildren("Spells");

            // Get the correct scream heads object and spawn it relative to the remote player
            var scrHeadsObject = localPlayerSpells.FindGameObjectInChildren(screamObjectName);
            var screamHeads = Object.Instantiate(
                scrHeadsObject,
                playerSpells.transform
            );
            screamHeads.SetActive(true);

            // We don't want to deactivate this when the local player is being hit 
            Object.Destroy(screamHeads.LocateMyFSM("Deactivate on Hit"));

            // For each (L, R and U) of the scream objects, we need to do a few things
            var objectNames = new string[] {"Hit L", "Hit R", "Hit U"};
            // Also store a few objects that we need to destroy later
            var objectsToDestroy = new List<GameObject>();
            foreach (var objectName in objectNames) {
                var screamHitObject = screamHeads.FindGameObjectInChildren(objectName);
                Object.Destroy(screamHitObject.LocateMyFSM("damages_enemy"));

                var screamHitDamager = Object.Instantiate(
                    new GameObject(objectName),
                    screamHitObject.transform
                );
                screamHitDamager.layer = 22;

                // Add the object to the list to destroy it later
                objectsToDestroy.Add(screamHitDamager);

                // Create a new polygon collider
                var screamHitDamagerPoly = screamHitDamager.AddComponent<PolygonCollider2D>();
                screamHitDamagerPoly.isTrigger = true;

                // Obtain the original polygon collider
                var screamHitPoly = screamHitObject.GetComponent<PolygonCollider2D>();

                // Copy over the polygon collider points
                screamHitDamagerPoly.points = screamHitPoly.points;
                
                // TODO: deal with PvP scenarios

                // Delete the original polygon collider, we don't need it anymore
                Object.Destroy(screamHitPoly);
            }

            // Wait for a second
            yield return new WaitForSeconds(1.0f);
            
            // Then destroy the leftover objects
            Object.Destroy(screamHeads);
            foreach (var gameObject in objectsToDestroy) {
                Object.Destroy(gameObject);
            }
        }
        
        public void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}