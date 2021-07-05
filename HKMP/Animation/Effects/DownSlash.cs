using UnityEngine;

namespace Hkmp.Animation.Effects {
    /**
     * The down slash animation (when the knight swings their nail downwards).
     */
    public class DownSlash : SlashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Call the base function with the correct parameters
            Play(playerObject, effectInfo, HeroController.instance.downSlashPrefab, SlashType.Down);
        }
    }
}