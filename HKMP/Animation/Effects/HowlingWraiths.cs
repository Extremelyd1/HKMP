using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the Howling Wraiths ability.
/// </summary>
internal class HowlingWraiths : ScreamBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        MonoBehaviourUtil.Instance.StartCoroutine(
            Play(playerObject, "Scream Antic1", "Scr Heads", ServerSettings.HowlingWraithDamage)
        );
    }
}
