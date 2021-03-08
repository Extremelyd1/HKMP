﻿using System.Collections;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

// TODO: perhaps play the screen shake also when our local player is close enough
namespace HKMP.Animation.Effects {
    public class DescendingDarkLand : AnimationEffect {
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
            var q2LandClip = (AudioClip) spellControl.GetAction<AudioPlay>("Q2 Land", 1).oneShotClip.Value;
            audioSource.PlayOneShot(q2LandClip);

            var localPlayerSpells = spellControl.gameObject;
            var playerSpells = playerObject.FindGameObjectInChildren("Spells");

            // Destroy the existing Q Trail from the down effect
            Object.Destroy(playerSpells.FindGameObjectInChildren("Q Trail 2"));

            // Obtain the Q Slam prefab and instantiate it relative to the player object
            // This is the shockwave that happens when you impact the ground,
            // slightly larger than the Desolate Dive one
            var qSlamObject = localPlayerSpells.FindGameObjectInChildren("Q Slam 2");
            var quakeSlam = Object.Instantiate(
                qSlamObject, 
                playerSpells.transform
            );
            quakeSlam.SetActive(true);
            quakeSlam.layer = 22;
            
            // If PvP is enabled add a DamageHero component to both hitbox sides
            var damage = GameSettings.DescendingDarkDamage;
            
            if (GameSettings.IsPvpEnabled && damage != 0) {
                quakeSlam.FindGameObjectInChildren("Hit L").AddComponent<DamageHero>().damageDealt = damage;
                quakeSlam.FindGameObjectInChildren("Hit R").AddComponent<DamageHero>().damageDealt = damage;
            }
            
            // The FSM has a Wait action of 0.75, but that is way too long
            yield return new WaitForSeconds(0.35f);
            
            // Obtain the Q Pillar prefab and instantiate it relative to the player object
            // This is the void-looking column when you impact the ground
            var qPillarObject = localPlayerSpells.FindGameObjectInChildren("Q Pillar");
            var quakePillar = Object.Instantiate(
                qPillarObject, 
                playerSpells.transform
            );
            quakePillar.SetActive(true);
            
            // Obtain the Q Mega prefab and instantiate it relative to the player object
            // This is the void tornado like effect around the knight when you impact the ground
            var qMegaObject = localPlayerSpells.FindGameObjectInChildren("Q Mega");
            var qMega = Object.Instantiate(
                qMegaObject, 
                playerSpells.transform
            );
            qMega.SetActive(true);
            // Play the Q Mega animation from the first frame
            qMega.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(0);
            
            // Enable the correct layer
            var qMegaHitL = qMega.FindGameObjectInChildren("Hit L");
            qMegaHitL.layer = 22;
            var qMegaHitR = qMega.FindGameObjectInChildren("Hit R");
            qMegaHitR.layer = 22;

            if (GameSettings.IsPvpEnabled && damage != 0) {
                qMegaHitL.AddComponent<DamageHero>().damageDealt = damage;
                qMegaHitR.AddComponent<DamageHero>().damageDealt = damage;
            }
            
            // Wait a second
            yield return new WaitForSeconds(1.0f);
            
            // And then destroy the remaining objects from the effect
            Object.Destroy(quakeSlam);
            Object.Destroy(quakePillar);
            Object.Destroy(qMega);
        }
    }
}