using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class when the trail from the Defenders Crest charm ends.
/// </summary>
internal class DungTrailEnd : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        // Try to find and destroy the dung particle if it exists 
        Object.Destroy(playerEffects.FindGameObjectInChildren("Dung Particle"));
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
