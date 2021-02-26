using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    /**
     * The up slash animation (when the knight swings their nail upwards).
     */
    public class UpSlash : SlashBase {
        public override void Play(GameObject playerObject, Packet packet) {
            // Call the base function with the correct parameters
            Play(playerObject, packet, HeroController.instance.upSlashPrefab, false, true, false);
        }
    }
}