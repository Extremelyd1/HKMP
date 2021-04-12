using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    /**
     * The up slash animation (when the knight swings their nail upwards).
     */
    public class UpSlash : SlashBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            // Call the base function with the correct parameters
            Play(playerObject, skin, effectInfo, HeroController.instance.upSlashPrefab, false, true, false);
        }
    }
}