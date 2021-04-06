using UnityEngine;
using HKMP.ServerKnights;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    /**
     * The wall slash animation (when the knight swings their nail into a wall).
     */
    public class WallSlash : SlashBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            // Call the base function with the correct parameters
            Play(playerObject, skin, effectInfo, HeroController.instance.wallSlashPrefab, false, false, true);
        }
    }
}