using System.Collections;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects {
    /// <summary>
    /// Animation effect class for hazard deaths.
    /// </summary>
    internal class HazardDeath : AnimationEffect {
        /// <summary>
        /// The fade out duration of the effect.
        /// </summary>
        private const float FadeOutDuration = 0.5f;

        /// <inheritdoc/>
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Get the effect info
            var hazardWasSpikes = effectInfo[0];
            var hazardWasAcid = effectInfo[1];

            // Remove all effects/attacks/spells related animations
            MonoBehaviourUtil.DestroyAllChildren(playerObject.FindGameObjectInChildren("Attacks"));
            MonoBehaviourUtil.DestroyAllChildren(playerObject.FindGameObjectInChildren("Effects"));
            MonoBehaviourUtil.DestroyAllChildren(playerObject.FindGameObjectInChildren("Spells"));

            // Disable the player object renderer so it isn't visible anymore
            playerObject.GetComponent<MeshRenderer>().enabled = false;

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

            // Start a coroutine for player the respawn animation
            MonoBehaviourUtil.Instance.StartCoroutine(WaitRespawnFromHazard(playerObject));
        }

        /// <inheritdoc/>
        public override bool[] GetEffectInfo() {
            return null;
        }

        /// <summary>
        /// Fades out the object with the given renderer over a duration.
        /// </summary>
        /// <param name="renderer">The renderer to fade out.</param>
        /// <param name="duration">The duration that the fade-out should take.</param>
        /// <returns>An enumerator for the coroutine.</returns>
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

        /// <summary>
        /// Waits the hazard death time and plays the respawn from hazard animations for the player.
        /// </summary>
        /// <param name="playerObject">The player object for which to play the animations.</param>
        /// <returns></returns>
        private IEnumerator WaitRespawnFromHazard(GameObject playerObject) {
            // Slightly longer delay than used in the game's coroutine, since locally the game fades out
            // so the character is already no longer visible, but for remote objects we need to hide it a little longer
            yield return new WaitForSeconds(0.9f);

            playerObject.GetComponent<MeshRenderer>().enabled = true;
        }
    }
}
