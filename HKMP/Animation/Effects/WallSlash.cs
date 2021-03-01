using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Animation.Effects {
    /**
     * The wall slash animation (when the knight swings their nail into a wall).
     */
    public class WallSlash : SlashBase {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Call the base function with the correct parameters
            Play(playerObject, packet, HeroController.instance.wallSlashPrefab, false, false, true);
        }
    }
}