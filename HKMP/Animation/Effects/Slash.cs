using UnityEngine;

namespace Hkmp.Animation.Effects {
    /**
     * The default slash animation (when the knight swings their nail).
     */
    public class Slash : SlashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Call the base function with the correct parameters
            Play(playerObject, effectInfo, HeroController.instance.slashPrefab, SlashType.Normal);
        }
    }
}