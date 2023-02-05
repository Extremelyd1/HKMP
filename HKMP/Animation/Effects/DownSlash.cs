using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for downwards nail slashes.
/// </summary>
internal class DownSlash : SlashBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Call the base function with the correct parameters
        Play(playerObject, effectInfo, HeroController.instance.downSlashPrefab, SlashType.Down);
    }
}
