using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Animation.Effects {
    /**
     * The up slash animation (when the knight swings their nail upwards).
     */
    public class UpSlash : SlashBase {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Call the base function with the correct parameters
            Play(playerObject, packet, HeroController.instance.upSlashPrefab, false, true, false);
        }
    }
}