using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for charging the Crystal Dash ability on the ground.
/// </summary>
internal class CrystalDashGroundCharge : CrystalDashChargeBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        Play(playerObject, "Ground Charge", 11);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
