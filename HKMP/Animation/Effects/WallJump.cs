using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for wall jumps.
/// </summary>
internal class WallJump : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        var wallPuffPrefab = HeroController.instance.wallPuffPrefab;
        var wallPuff = Object.Instantiate(
            wallPuffPrefab,
            playerEffects.transform
        );
        wallPuff.SetActive(true);

        // Invert the x scale, because for some reason, the effect is flipped by default
        var puffScale = wallPuff.transform.localScale;
        wallPuff.transform.localScale = new Vector3(
            puffScale.x * -1f,
            puffScale.y,
            puffScale.z
        );

        // This means that the reposition action in the FSM is also inverted,
        // so we subtract twice the amount that was added
        wallPuff.LocateMyFSM("FSM").InsertMethod("Reposition?", 3, () => {
            var position = wallPuff.transform.localPosition;
            wallPuff.transform.localPosition = position - new Vector3(
                0.91f * 2,
                0f,
                0f
            );
        });

        // Get a new audio source object relative to the player object
        var wallJumpAudioObject = AudioUtil.GetAudioSourceObject(playerEffects);
        // Get the actual audio source
        var wallJumpAudioSource = wallJumpAudioObject.GetComponent<AudioSource>();

        // Get the wall jump clip
        var heroAudioController = HeroController.instance.GetComponent<HeroAudioController>();
        wallJumpAudioSource.clip = heroAudioController.walljump.clip;
        // Randomize the pitch, as in the HeroAudioController
        wallJumpAudioSource.pitch = Random.Range(0.9f, 1.1f);
        // Play it
        wallJumpAudioSource.Play();

        // Destroy the objects after 2 seconds to prevent them from lingering around
        Object.Destroy(wallPuff, 2.0f);
        Object.Destroy(wallJumpAudioObject, 2.0f);
    }

    public override bool[] GetEffectInfo() {
        return null;
    }
}
