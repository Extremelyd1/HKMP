using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    /**
     * The wall slash animation (when the knight swings their nail into a wall).
     */
    public class WallSlash : SlashBase {
        public override void Play(GameObject playerObject, Packet packet) {
            // Call the base function with the correct parameters
            Play(playerObject, packet, HeroController.instance.wallSlashPrefab, false, false, true);
        }
    }
}