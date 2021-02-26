using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    /**
     * The down slash animation (when the knight swings their nail downwards).
     */
    public class DownSlash : SlashBase {
        public override void Play(GameObject playerObject, Packet packet) {
            // Call the base function with the correct parameters
            Play(playerObject, packet, HeroController.instance.downSlashPrefab, true, false, false);
        }
    }
}