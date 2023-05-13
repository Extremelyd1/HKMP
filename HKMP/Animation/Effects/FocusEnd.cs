using System.Collections;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using FadeAudio = Hkmp.Fsm.FadeAudio;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the end of the focus animation (either when fully healed or when cancelled).
/// </summary>
internal class FocusEnd : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        Play(playerObject);
    }

    /// <summary>
    /// Plays the animation effect for the given player object.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    public void Play(GameObject playerObject) {
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        // Get the audio for the charge that is playing
        var chargeAudio = playerEffects.FindGameObjectInChildren("Charge Audio");
        if (chargeAudio != null) {
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
        }

        // Disable this warning since it is inherent to the Hollow Knight source, so we can't work around it
#pragma warning disable 0618
        // Get the cached dust particles and disable them by setting their emission rate to zero
        var dustL = playerEffects.FindGameObjectInChildren("Dust L");
        if (dustL != null) {
            dustL.GetComponent<ParticleSystem>().emissionRate = 0;
        }

        var dustR = playerEffects.FindGameObjectInChildren("Dust R");
        if (dustR != null) {
            dustR.GetComponent<ParticleSystem>().emissionRate = 0;
        }
#pragma warning restore 0618

        // Start a coroutine for playing the end animation
        MonoBehaviourUtil.Instance.StartCoroutine(PlayEndAnimation(playerEffects));

        var shellAnimation = playerEffects.FindGameObjectInChildren("Shell Animation");
        if (shellAnimation == null) {
            shellAnimation = playerEffects.FindGameObjectInChildren("Shell Animation Last");
        }

        if (shellAnimation != null) {
            var shellAnimator = shellAnimation.GetComponent<tk2dSpriteAnimator>();
            shellAnimator.Play("Disappear");

            // Destroy it after the animation is done
            Object.Destroy(shellAnimation, shellAnimator.GetClipByName("Disappear").Duration);
        }

        var audioObject = playerEffects.FindGameObjectInChildren("Shell Audio");
        if (audioObject != null) {
            var audioSource = audioObject.GetComponent<AudioSource>();

            var charmEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Charm Effects");
            var blockerShieldObject = charmEffects.FindGameObjectInChildren("Blocker Shield");
            var shellFsm = blockerShieldObject.LocateMyFSM("Control");

            var audioPlayAction = shellFsm.GetFirstAction<AudioPlayerOneShotSingle>("Focus End");
            audioSource.clip = (AudioClip) audioPlayAction.audioClip.Value;
            audioSource.Play();

            // Destroy it after the audio clip is done
            // Object.Destroy(audioObject, audioSource.clip.length);
        }
    }

    /// <summary>
    /// Stop the audio after the focus delay.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    /// <param name="chargeAudio">The GameObject for the charge audio.</param>
    /// <param name="audioSource">The audio source for the focus audio.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator StopAudio(GameObject playerObject, GameObject chargeAudio, AudioSource audioSource) {
        // Get the sprite animator and retrieve the duration of the Focus End animation
        var animator = playerObject.GetComponent<tk2dSpriteAnimator>();
        var focusEndAnimationDuration = animator.GetClipByName("Focus End").Duration;

        // Wait for the duration of the animation
        yield return new WaitForSeconds(focusEndAnimationDuration);

        // Now stop the audio and destroy the charge object
        if (audioSource != null) {
            audioSource.Stop();
        }

        Object.Destroy(chargeAudio);
    }

    /// <summary>
    /// Plays the focus end animation.
    /// </summary>
    /// <param name="playerEffects">The GameObject for the player effects of the player.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator PlayEndAnimation(GameObject playerEffects) {
        // Get the cached lines animation from the player object
        var linesAnimation = playerEffects.FindGameObjectInChildren("Lines Anim");
        if (linesAnimation != null) {
            // Get the sprite animator and play the Focus Effect End animation
            linesAnimation.GetComponent<tk2dSpriteAnimator>().Play("Focus Effect End");

            // Wait for this duration that is defined in the FSM
            yield return new WaitForSeconds(0.23f);

            // Disable the renderer for the lines
            if (linesAnimation != null) {
                linesAnimation.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
