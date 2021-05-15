using System.Collections;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

// TODO: perhaps play the screen shake also when our local player is close enough
namespace HKMP.Animation.Effects {
    public class DesolateDiveLand : DamageAnimationEffect {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            MonoBehaviourUtil.Instance.StartCoroutine(PlayEffectInCoroutine(playerObject));
        }

        public override bool[] GetEffectInfo() {
            return null;
        }

        private IEnumerator PlayEffectInCoroutine(GameObject playerObject) {
            var spellControl = HeroController.instance.spellControl;
            
            // Get an audio source
            var audioObject = AudioUtil.GetAudioSourceObject(playerObject);
            var audioSource = audioObject.GetComponent<AudioSource>();
            
            // Find the land clip and play it
            var qLandClip = (AudioClip) spellControl.GetAction<AudioPlay>("Quake1 Land", 1).oneShotClip.Value;
            audioSource.PlayOneShot(qLandClip);
            
            // Destroy the audio object after the clip is done
            Object.Destroy(audioObject, qLandClip.length);

            var localPlayerSpells = spellControl.gameObject;
            var playerSpells = playerObject.FindGameObjectInChildren("Spells");

            // Destroy the existing Q Trail from the down effect
            Object.Destroy(playerSpells.FindGameObjectInChildren("Q Trail"));

            // Obtain the Q Slam prefab and instantiate it relative to the player object
            // This is the shockwave that happens when you impact the ground
            var qSlamObject = localPlayerSpells.FindGameObjectInChildren("Q Slam");
            var quakeSlam = Object.Instantiate(
                qSlamObject, 
                playerSpells.transform
            );
            quakeSlam.SetActive(true);
            
            // If PvP is enabled add a DamageHero component to both hitbox sides
            var damage = GameSettings.DesolateDiveDamage;
            
            if (GameSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
                quakeSlam.FindGameObjectInChildren("Hit L").AddComponent<DamageHero>().damageDealt = damage;
                quakeSlam.FindGameObjectInChildren("Hit R").AddComponent<DamageHero>().damageDealt = damage;
            }
            
            // Obtain the Q1 Pillar prefab and instantiate it relative to the player object
            // This is the curvy pillar that comes from the sky once you impact the ground
            var qPillarObj = localPlayerSpells.FindGameObjectInChildren("Q1 Pillar");
            var quakePillar = Object.Instantiate(
                qPillarObj, 
                playerSpells.transform
            );
            quakePillar.SetActive(true);
            
            // Wait a second
            yield return new WaitForSeconds(1.0f);
            //
            // // And then destroy the remaining objects from the effect
            Object.Destroy(quakeSlam);
            Object.Destroy(quakePillar);
        }
    }
}