using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for a normal left or right dash.
/// </summary>
internal class Dash : DashBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        Play(playerObject, effectInfo, false, false, false);
    }
}
