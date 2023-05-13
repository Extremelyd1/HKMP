using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for a Sharp Shadow Dash.
/// </summary>
internal class ShadowDashSharp : DashBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        Play(playerObject, effectInfo, true, true, false);
    }
}
