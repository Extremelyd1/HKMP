using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    /**
     * The down slash animation (when the knight swings their nail downwards).
     */
    public class DownSlash : SlashBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            // Call the base function with the correct parameters
            Play(playerObject, skin, effectInfo, HeroController.instance.downSlashPrefab, true, false, false);
        }
    }
}