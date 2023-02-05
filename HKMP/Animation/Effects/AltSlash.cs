using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the alternative slash animation (when the knight swings their nail).
/// This is the slash effect that occurs the most.
/// </summary>
internal class AltSlash : SlashBase {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Call the base function with the correct parameters
        Play(playerObject, effectInfo, HeroController.instance.slashAltPrefab, SlashType.Alt);
    }
}
