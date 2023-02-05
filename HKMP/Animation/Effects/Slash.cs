using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the normal nail slash.
/// </summary>
internal class Slash : SlashBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Call the base function with the correct parameters
        Play(playerObject, effectInfo, HeroController.instance.slashPrefab, SlashType.Normal);
    }
}
