using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for a downwards dash.
/// </summary>
internal class DashDown : DashBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        Play(playerObject, effectInfo, false, false, true);
    }
}
