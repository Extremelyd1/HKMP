using System.Collections;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class HazardDeath : AnimationEffect {
        private const float FadeOutDuration = 0.5f;
    
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Get the effect info
            var hazardWasSpikes = effectInfo[0];
            var hazardWasAcid = effectInfo[1];
            
            // Disable the player object so it isn't visible anymore
            playerObject.SetActive(false);

            if (hazardWasSpikes) {
                // Spawn the spike death object relative to the player object
                var spikeDeathPrefab = HeroController.instance.spikeDeathPrefab;
                var spikeDeath = spikeDeathPrefab.Spawn(playerObject.transform.position);

                var spikeDeathFsm = spikeDeath.LocateMyFSM("Knight Death Control");

                // Get the audio play action and change the spawn point of the audio to be the player object
                var audioPlayAction = spikeDeathFsm.GetAction<AudioPlayerOneShot>("Stab", 4);
                audioPlayAction.spawnPoint.Value = playerObject;
                
                // Remove the screen shake effect
                spikeDeathFsm.GetAction<SendEventByName>("Stab", 8).sendEvent.Value = "";

                // Start a coroutine to fade out the spike death object
                MonoBehaviourUtil.Instance.StartCoroutine(FadeObjectOut(
                    spikeDeath.GetComponent<MeshRenderer>(),
                    FadeOutDuration
                ));

                // Set the spike direction to the default value as in the HeroController
                FSMUtility.SetFloat(spikeDeath.GetComponent<PlayMakerFSM>(), "Spike Direction", 57.29578f);

                // Destroy it after some time
                Object.Destroy(spikeDeath, FadeOutDuration);
            } else if (hazardWasAcid) {
                // Spawn the acid death object relative to the player object
                var acidDeathPrefab = HeroController.instance.acidDeathPrefab;
                var acidDeath = acidDeathPrefab.Spawn(playerObject.transform.position);
                
                var acidDeathFsm = acidDeath.LocateMyFSM("Knight Acid Death");
                
                // Get the audio play action and change the spawn point of the audio to be the player object
                var damagePlayAction = acidDeathFsm.GetAction<AudioPlayerOneShot>("Effects", 2);
                damagePlayAction.spawnPoint.Value = playerObject;
                // There is another one that plays the splash sound, also change the spawn point
                var splashPlayAction = acidDeathFsm.GetAction<AudioPlayerOneShot>("Effects", 3);
                // Also change the audio player, otherwise the sound will play at the local player
                splashPlayAction.audioPlayer = damagePlayAction.audioPlayer;
                splashPlayAction.spawnPoint.Value = playerObject;

                // Remove the screen shake effect
                acidDeathFsm.GetAction<SendEventByName>("Effects", 5).sendEvent.Value = "";
                
                // Start a coroutine to fade out the spike death object
                MonoBehaviourUtil.Instance.StartCoroutine(FadeObjectOut(
                    acidDeath.GetComponent<MeshRenderer>(), 
                    FadeOutDuration
                ));
                
                // Set the scale to the player scale
                acidDeath.transform.localScale = playerObject.transform.localScale;

                // Destroy it after some time
                Object.Destroy(acidDeath, FadeOutDuration);
            }
        }

        public override bool[] GetEffectInfo() {
            return null;
        }

        private IEnumerator FadeObjectOut(Renderer renderer, float duration) {
            for (var t = 0f; t < duration; t += Time.deltaTime) {
                var normalizedTime = t / duration;
                var alpha = 1f - normalizedTime;

                var material = renderer.material;
                var color = material.color;
                material.color = new Color(color.r, color.g, color.b, alpha);
                
                yield return null;
            }
        }
    }
}