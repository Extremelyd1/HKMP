using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation.Effects {
    /**
     * The default slash animation (when the knight swings their nail).
     */
    public class Slash : SlashBase {
        public override void Play(GameObject playerObject, Packet packet) {
            // Call the base function with the correct parameters
            Play(playerObject, packet, HeroController.instance.slashPrefab, false, false, false);
        }
    }
}