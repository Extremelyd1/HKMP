using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class HardLand : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");

            var hardLandingEffectPrefab = HeroController.instance.hardLandingEffectPrefab;
            if (hardLandingEffectPrefab != null) {
                var hardLandingEffect = hardLandingEffectPrefab.Spawn(playerEffects.transform.position);
                Object.Destroy(hardLandingEffect, 3.0f);
            }

            // Get a new audio source object relative to the player object
            var hardLandAudioObject = AudioUtil.GetAudioSourceObject(playerEffects);
            // Get the actual audio source
            var hardLandAudioSource = hardLandAudioObject.GetComponent<AudioSource>();
            
            // Get the wall slide clip and play it
            var heroAudioController = HeroController.instance.GetComponent<HeroAudioController>();
            if (heroAudioController != null) {
                hardLandAudioSource.clip = heroAudioController.hardLanding.clip;
                hardLandAudioSource.Play();
            }

            Object.Destroy(hardLandAudioObject, 3.0f);
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}