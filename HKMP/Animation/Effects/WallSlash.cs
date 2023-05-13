using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for nail slashes while on a wall.
/// </summary>
internal class WallSlash : SlashBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Call the base function with the correct parameters
        Play(playerObject, effectInfo, HeroController.instance.wallSlashPrefab, SlashType.Wall);
    }
}
