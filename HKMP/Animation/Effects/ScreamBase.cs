using System.Collections;
using System.Collections.Generic;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    public abstract class ScreamBase : DamageAnimationEffect {
        public abstract override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo);

        protected IEnumerator Play(GameObject playerObject, clientSkin skin, string screamClipName, string screamObjectName, int damage) {
            var spellControl = HeroController.instance.spellControl;

            var audioObject = AudioUtil.GetAudioSourceObject(playerObject);
            var audioSource = audioObject.GetComponent<AudioSource>();

            // Get the correct scream audio clip based on the given parameter
            var screamClip = (AudioClip) spellControl.GetAction<AudioPlay>(screamClipName, 1).oneShotClip.Value;
            // And play it
            audioSource.PlayOneShot(screamClip);
            
            // Destroy the audio object after the clip is done
            Object.Destroy(audioObject, screamClip.length);
            
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
            var objectNames = new [] {"Hit L", "Hit R", "Hit U"};
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
                
                // If PvP is enabled, add a DamageHero component to the damager objects
                if (GameSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
                    screamHitDamager.AddComponent<DamageHero>().damageDealt = damage;
                }

                // Delete the original polygon collider, we don't need it anymore
                Object.Destroy(screamHitPoly);
            }

            // Wait for the duration of the scream animation
            var duration = playerObject.GetComponent<tk2dSpriteAnimator>().GetClipByName("Scream 2 Get")
                .Duration;
            yield return new WaitForSeconds(duration);
            
            // Then destroy the leftover objects
            Object.Destroy(screamHeads);
            foreach (var gameObject in objectsToDestroy) {
                Object.Destroy(gameObject);
            }
        }
        
        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}