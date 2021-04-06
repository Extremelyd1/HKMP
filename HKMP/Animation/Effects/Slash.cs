using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    /**
     * The default slash animation (when the knight swings their nail).
     */
    public class Slash : SlashBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            // Call the base function with the correct parameters
            Play(playerObject, skin, effectInfo, HeroController.instance.slashPrefab, false, false, false);
        }
    }
}