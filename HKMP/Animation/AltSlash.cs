using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    /**
     * The alternative slash animation (when the knight swings their nail).
     * This is the one that occurs the most
     */
    public class AltSlash : SlashBase {
        public override void Play(GameObject playerObject, Packet packet) {
            // Call the base function with the correct parameters
            Play(playerObject, packet, HeroController.instance.slashAltPrefab, false, false, false);
        }
    }
}