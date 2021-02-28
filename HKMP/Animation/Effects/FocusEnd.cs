using System.Collections;
using HKMP.Fsm;
using HKMP.Networking.Packet;
using HKMP.Util;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation.Effects {
    /**
     * End of the healing animation of the knight, either when cancelled or when fully restored
     */
    public class FocusEnd : IAnimationEffect {
        public void Play(GameObject playerObject, Packet packet) {
            // Get the audio for the charge that is playing
            var chargeAudio = playerObject.FindGameObjectInChildren("Charge Audio");
            var audioSource = chargeAudio.GetComponent<AudioSource>();

            // Instantiate a custom fade audio object
            var fadeAudio = new FadeAudio(
                audioSource,
                1,
                0,
                0.33f
            );

            // Make sure our fade audio object updates from the Unity update loop
            MonoBehaviourUtil.Instance.OnUpdateEvent += fadeAudio.Update;

            // Start a coroutine to stop the audio once it has faded
            MonoBehaviourUtil.Instance.StartCoroutine(StopAudio(playerObject, chargeAudio, audioSource));

            // Disable this warning since it is inherent to the Hollow Knight source, so we can't work around it
#pragma warning disable 0618
            // Get the cached dust particles and disable them by setting their emission rate to zero
            var dustL = playerObject.FindGameObjectInChildren("Dust L");
            dustL.GetComponent<ParticleSystem>().emissionRate = 0;
            var dustR = playerObject.FindGameObjectInChildren("Dust R");
            dustR.GetComponent<ParticleSystem>().emissionRate = 0;
#pragma warning restore 0618

            // Start a coroutine for playing the end animation
            MonoBehaviourUtil.Instance.StartCoroutine(PlayEndAnimation(playerObject));
        }

        private IEnumerator StopAudio(GameObject playerObject, GameObject chargeAudio, AudioSource audioSource) {
            // Get the sprite animator and retrieve the duration of the Focus End animation
            var animator = playerObject.GetComponent<tk2dSpriteAnimator>();
            var focusEndAnimationDuration = animator.GetClipByName("Focus End").Duration;
            
            // Wait for the duration of the animation
            yield return new WaitForSeconds(focusEndAnimationDuration);
            
            // Now stop the audio and destroy the charge object
            audioSource.Stop();
            Object.Destroy(chargeAudio);
        }

        private IEnumerator PlayEndAnimation(GameObject playerObject) {
            // Get the cached lines animation from the player object
            var linesAnimation = playerObject.FindGameObjectInChildren("Lines Anim");
            // Get the sprite animator and play the Focus Effect End animation
            linesAnimation.GetComponent<tk2dSpriteAnimator>().Play("Focus Effect End");

            // Wait for this duration that is defined in the FSM
            yield return new WaitForSeconds(0.23f);

            // Disable the renderer for the lines
            linesAnimation.GetComponent<MeshRenderer>().enabled = false;
        }

        public void PreparePacket(Packet packet) {
        }
    }
}