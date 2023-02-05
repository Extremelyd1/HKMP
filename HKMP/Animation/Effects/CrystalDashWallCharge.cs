using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for charging the Crystal Dash ability on the wall.
/// </summary>
internal class CrystalDashWallCharge : CrystalDashChargeBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        Play(playerObject, "Wall Charge", 16);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
