using UnityEngine;

namespace Hkmp.Animation.Effects {
    /**
     * The up slash animation (when the knight swings their nail upwards).
     */
    public class UpSlash : SlashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Call the base function with the correct parameters
            Play(playerObject, effectInfo, HeroController.instance.upSlashPrefab, SlashType.Up);
        }
    }
}