using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Animation.Effects {
    /**
     * The down slash animation (when the knight swings their nail downwards).
     */
    public class DownSlash : SlashBase {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Call the base function with the correct parameters
            Play(playerObject, packet, HeroController.instance.downSlashPrefab, true, false, false);
        }
    }
}