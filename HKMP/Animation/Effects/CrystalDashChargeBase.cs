using System.Collections;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Abstract base class for animation effect of charging the Crystal Dash ability.
/// </summary>
internal abstract class CrystalDashChargeBase : AnimationEffect {
    /// <inheritdoc/>
    public abstract override void Play(GameObject playerObject, bool[] effectInfo);

    /// <summary>
    /// Play the animation effect for the given player object, a given state name in the charge FSM and
    /// the index of the charge effect.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    /// <param name="chargeStateName">The name of the state in the charge FSM.</param>
    /// <param name="chargeEffectIndex">The index of the charge effect in the FSM.</param>
    protected void Play(GameObject playerObject, string chargeStateName, int chargeEffectIndex) {
        var coroutine =
            MonoBehaviourUtil.Instance.StartCoroutine(PlayAnimation(playerObject, chargeStateName,
                chargeEffectIndex));

        playerObject.GetComponent<CoroutineCancelComponent>().AddCoroutine("Crystal Dash Charge", coroutine);
    }

    /// <summary>
    /// Play the animation effect for the given player object, a given state name in the charge FSM and
    /// the index of the charge effect.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    /// <param name="chargeStateName">The name of the state in the charge FSM.</param>
    /// <param name="chargeEffectIndex">The index of the charge effect in the FSM.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator PlayAnimation(GameObject playerObject, string chargeStateName, int chargeEffectIndex) {
        // Get the Superdash FSM from the HeroController
        var superDashFsm = HeroController.instance.gameObject.LocateMyFSM("Superdash");

        // Check whether the player already has a superdash charge audio object
        var chargeAudioObject = playerObject.FindGameObjectInChildren("Superdash Charge Audio");
        if (chargeAudioObject == null) {
            // There is not object yet, so we create one by finding the clip in the FSM
            var chargeAudioPlay = superDashFsm.GetFirstAction<AudioPlay>("Ground Charge");

            var chargeAudioSource = chargeAudioPlay.gameObject.GameObject.Value.GetComponent<AudioSource>();

            // Create a fresh AudioSource object
            chargeAudioObject = AudioUtil.GetAudioSourceObject(playerObject);
            chargeAudioObject.name = "Superdash Charge Audio";
            // Copy over the audio clip
            chargeAudioObject.GetComponent<AudioSource>().clip = chargeAudioSource.clip;
        }

        // Play the charge sound
        chargeAudioObject.GetComponent<AudioSource>().Play();

        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        // Find the charge effect, which is the circular vortex motion around the knight when he charges
        var chargeEffectObject = superDashFsm.GetAction<SetMeshRenderer>(chargeStateName, chargeEffectIndex);
        var chargeEffect = Object.Instantiate(
            chargeEffectObject.gameObject.GameObject.Value,
            playerEffects.transform
        );

        // Assign a name, so we can reference it later
        chargeEffect.name = "Charge Effect";
        chargeEffect.GetComponent<MeshRenderer>().enabled = true;

        // Make sure it plays the animation
        var chargeSpriteAnimator = chargeEffect.GetComponent<tk2dSpriteAnimator>();
        chargeSpriteAnimator.PlayFromFrame("SD Fx Charge", 0);

        // Destroy it after the duration of the animation is over
        Object.Destroy(chargeEffect, chargeSpriteAnimator.GetClipByName("SD Fx Charge").Duration);

        // Wait for the duration of the crystal dash charge
        yield return new WaitForSeconds(0.8f);

        // Find the bling effect in the FSM and instantiate it
        var blingEffectObject = superDashFsm.GetFirstAction<ActivateGameObject>("Ground Charged");
        var blingEffect = Object.Instantiate(
            blingEffectObject.gameObject.GameObject.Value,
            playerEffects.transform
        );

        blingEffect.SetActive(true);

        // It's a short effect, so we can destroy it quickly
        Object.Destroy(blingEffect, 1.0f);

        // We are done, so we can cancel the coroutine
        playerObject.GetComponent<CoroutineCancelComponent>().CancelCoroutine("Crystal Dash Charge");
    }

    /// <inheritdoc/>
    public abstract override bool[] GetEffectInfo();
}
