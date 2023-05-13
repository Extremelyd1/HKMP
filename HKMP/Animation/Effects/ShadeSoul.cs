using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the Shade Soul ability.
/// </summary>
internal class ShadeSoul : FireballBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Call the base play method with the correct indices and state names
        // This looks arbitrary, but is based on the FSM state machine of the fireball
        Play(
            playerObject,
            effectInfo,
            "Fireball 2",
            1,
            1.8f,
            false,
            ServerSettings.ShadeSoulDamage
        );
    }
}
