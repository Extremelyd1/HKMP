using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the Abyss Shriek ability.
/// </summary>
internal class AbyssShriek : ScreamBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        MonoBehaviourUtil.Instance.StartCoroutine(
            Play(playerObject, "Scream Antic2", "Scr Heads 2", ServerSettings.AbyssShriekDamage)
        );
    }
}
