using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the Monarch Wings ability.
/// </summary>
internal class MonarchWings : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        // Find and spawn the wings object
        var doubleJumpWingsPrefab = HeroController.instance.dJumpWingsPrefab;
        var doubleJumpWings = Object.Instantiate(
            doubleJumpWingsPrefab,
            playerEffects.transform
        );
        doubleJumpWings.SetActive(true);

        // Find and spawn the flash object
        var doubleJumpFlashPrefab = HeroController.instance.dJumpFlashPrefab;
        var doubleJumpFlash = Object.Instantiate(
            doubleJumpFlashPrefab,
            playerEffects.transform
        );

        doubleJumpFlash.SetActive(true);

        // Find and spawn the feathers particle system
        var doubleJumpFeathersObject = HeroController.instance.dJumpFeathers;
        var doubleJumpFeathers = Object.Instantiate(
            doubleJumpFeathersObject,
            playerEffects.transform
        );

        doubleJumpFeathers.Play();

        // Get a new audio source object relative to the player object
        var doubleJumpAudioObject = AudioUtil.GetAudioSourceObject(playerEffects);
        // Get the actual audio source
        var doubleJumpAudioSource = doubleJumpAudioObject.GetComponent<AudioSource>();

        // Get the wall slide clip and play it
        doubleJumpAudioSource.PlayOneShot(HeroController.instance.doubleJumpClip);

        // Destroy all objects after 2 seconds, which is when every effect should be done
        Object.Destroy(doubleJumpWings, 2.0f);
        Object.Destroy(doubleJumpFlash, 2.0f);
        Object.Destroy(doubleJumpFeathers, 2.0f);
        Object.Destroy(doubleJumpAudioObject, 2.0f);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
