using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the upwards nail swing.
/// </summary>
internal class UpSlash : SlashBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Call the base function with the correct parameters
        Play(playerObject, effectInfo, HeroController.instance.upSlashPrefab, SlashType.Up);
    }
}
