using System.Collections;
using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

// TODO: perhaps play the screen shake also when our local player is close enough
namespace HKMP.Animation.Effects {
    public class DesolateDiveLand : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            MonoBehaviourUtil.Instance.StartCoroutine(PlayEffectInCoroutine(playerObject));
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }

        private IEnumerator PlayEffectInCoroutine(GameObject playerObject) {
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
            
            // Find the land clip and play it
            var qLandClip = (AudioClip) spellControl.GetAction<AudioPlay>("Quake1 Land", 1).oneShotClip.Value;
            audioSource.PlayOneShot(qLandClip);

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
            quakeSlam.layer = 22;
            
            // If PvP is enabled add a DamageHero component to both hitbox sides
            if (GameSettings.IsPvpEnabled) {
                quakeSlam.FindGameObjectInChildren("Hit L").AddComponent<DamageHero>();
                quakeSlam.FindGameObjectInChildren("Hit R").AddComponent<DamageHero>();
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