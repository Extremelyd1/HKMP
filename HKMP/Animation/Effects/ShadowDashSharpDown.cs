using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for a downwards Sharp Shadow Dash
/// </summary>
internal class ShadowDashSharpDown : DashBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        Play(playerObject, effectInfo, true, true, true);
    }
}
