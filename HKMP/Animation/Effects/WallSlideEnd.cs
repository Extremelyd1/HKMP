using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the end of a wall slide.
/// </summary>
internal class WallSlideEnd : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        // Find the dust object and disable emission it if it exists
        var wallSlideDustObject = playerEffects.FindGameObjectInChildren("Wall Slide Dust");
        if (wallSlideDustObject != null) {
#pragma warning disable 0618
            wallSlideDustObject.GetComponent<ParticleSystem>().enableEmission = false;
#pragma warning restore 0618
        }

        var audioObject = playerEffects.FindGameObjectInChildren("Wall Slide Audio");
        if (audioObject != null) {
            Object.Destroy(audioObject);
        }
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
