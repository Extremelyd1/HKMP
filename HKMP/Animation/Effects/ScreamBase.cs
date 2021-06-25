using System.Collections;
using System.Collections.Generic;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects {
    public abstract class ScreamBase : DamageAnimationEffect {
        public abstract override void Play(GameObject playerObject, bool[] effectInfo);

        protected IEnumerator Play(GameObject playerObject, string screamClipName, string screamObjectName,
            int damage) {
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

            // If PvP is enabled, add a DamageHero component to the damager objects
            if (GameSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
                // For each (L, R and U) of the scream objects, we need to add a DamageHero component
                var objectNames = new[] {"Hit L", "Hit R", "Hit U"};

                foreach (var objectName in objectNames) {
                    var screamHitObject = screamHeads.FindGameObjectInChildren(objectName);
                    
                    screamHitObject.AddComponent<DamageHero>().damageDealt = damage;
                }
            }

            // Wait for the duration of the scream animation
            var duration = playerObject
                .GetComponent<tk2dSpriteAnimator>()
                .GetClipByName("Scream 2 Get")
                .Duration;
            yield return new WaitForSeconds(duration);

            // Then destroy the leftover objects
            Object.Destroy(screamHeads);
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}