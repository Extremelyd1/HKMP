using HKMP.Util;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class WallSlide : AnimationEffect {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Also play the crystal dash cancel animation, because it is cancelled when we do a wallslide
            AnimationManager.CrystalDashChargeCancel.Play(playerObject, effectInfo);
            
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");

            // Find an existing dust object
            var wallSlideDustObject = playerEffects.FindGameObjectInChildren("Wall Slide Dust");

            // Otherwise, create a new one from the prefab in the HeroController
            if (wallSlideDustObject == null) {
                var wallSlideDustPrefab = HeroController.instance.wallslideDustPrefab.gameObject;
                wallSlideDustObject = Object.Instantiate(
                    wallSlideDustPrefab,
                    playerEffects.transform
                );
                // Give it a name, so we can find it later
                wallSlideDustObject.name = "Wall Slide Dust";
            }
            
            // Disable compiler warning and enable dust emission
#pragma warning disable 0618
            wallSlideDustObject.GetComponent<ParticleSystem>().enableEmission = true;
#pragma warning restore 0618
            
            // Get a new audio source object relative to the player object
            var wallSlideAudioObject = AudioUtil.GetAudioSourceObject(playerEffects);
            // Again give a name, so we can destroy it later
            wallSlideAudioObject.name = "Wall Slide Audio";
            // Get the actual audio source
            var wallSlideAudioSource = wallSlideAudioObject.GetComponent<AudioSource>();
            
            // Get the wall slide clip and play it
            var heroAudioController = HeroController.instance.GetComponent<HeroAudioController>();
            wallSlideAudioSource.clip = heroAudioController.wallslide.clip;
            wallSlideAudioSource.Play();
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}