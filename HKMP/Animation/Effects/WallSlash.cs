using UnityEngine;

namespace HKMP.Animation.Effects {
    /**
     * The wall slash animation (when the knight swings their nail into a wall).
     */
    public class WallSlash : SlashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Call the base function with the correct parameters
            Play(playerObject, effectInfo, HeroController.instance.wallSlashPrefab, SlashType.Wall);
        }
    }
}