using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the Vengeful Spirit ability.
/// </summary>
internal class VengefulSpirit : FireballBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Call the base play method with the correct indices and state names
        // This looks arbitrary, but is based on the FSM state machine of the fireball
        Play(
            playerObject,
            effectInfo,
            "Fireball 1",
            0,
            1.0f,
            true,
            ServerSettings.VengefulSpiritDamage
        );
    }
}
