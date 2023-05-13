using UnityEngine;

namespace Hkmp.Animation;

/// <summary>
/// Abstract base class for animation effects that can deal damage to other players.
/// </summary>
internal abstract class DamageAnimationEffect : AnimationEffect {
    /// <summary>
    /// Whether this effect should deal damage.
    /// </summary>
    protected bool ShouldDoDamage;

    /// <inheritdoc/>
    public abstract override void Play(GameObject playerObject, bool[] effectInfo);

    /// <inheritdoc/>
    public abstract override bool[] GetEffectInfo();

    /// <summary>
    /// Sets whether this animation effect should deal damage.
    /// </summary>
    /// <param name="shouldDoDamage">The new boolean value.</param>
    public void SetShouldDoDamage(bool shouldDoDamage) {
        ShouldDoDamage = shouldDoDamage;
    }
}
