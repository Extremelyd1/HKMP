using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for a fully charged nail art.
/// </summary>
internal class NailArtCharged : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Get the player attacks object
        var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");

        // If the art charge object exists, destroy it
        var artCharge = playerAttacks.FindGameObjectInChildren("Nail Art Charge");
        if (artCharge != null) {
            Object.Destroy(artCharge);
        }

        // If we already have a charge effect, we skip creating another one
        if (playerAttacks.FindGameObjectInChildren("Nail Art Charged") != null) {
            return;
        }

        // Create a new art charged object from the prefab in the hero controller
        var artChargedObject = HeroController.instance.artChargedEffect;
        var artCharged = Object.Instantiate(
            artChargedObject,
            playerAttacks.transform
        );
        // Give it a name, so we can reference it when it needs to be destroyed
        artCharged.name = "Nail Art Charged";
        // Set is to active to activate the animation
        artCharged.SetActive(true);

        // Also play the animation
        artCharged.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(0);

        // Create a new art charge flash object
        var artChargedFlashObject = HeroController.instance.artChargedFlash;
        var artChargedFlash = Object.Instantiate(
            artChargedFlashObject,
            playerAttacks.transform
        );
        // Give it a name, so we can reference it when it needs to be destroyed
        artChargedFlash.name = "Nail Art Charged Flash";
        // Set is to active to activate the flash
        artChargedFlash.SetActive(true);

        // Get a new audio source object relative to the player object
        var artChargedAudioObject = AudioUtil.GetAudioSourceObject(playerAttacks);
        // Again give a name, so we can destroy it later
        artChargedAudioObject.name = "Nail Art Charged Audio";
        // Get the actual audio source
        var artChargedAudioSource = artChargedAudioObject.GetComponent<AudioSource>();

        // Get the nail art ready clip and play it
        var heroAudioController = HeroController.instance.GetComponent<HeroAudioController>();
        artChargedAudioSource.clip = heroAudioController.nailArtReady.clip;
        artChargedAudioSource.Play();
        // Also play the one shot clip of the nail art charge complete clip
        artChargedAudioSource.PlayOneShot(HeroController.instance.nailArtChargeComplete);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
