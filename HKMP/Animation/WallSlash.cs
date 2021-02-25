using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    public class WallSlash : SlashBase {
        public override void Play(GameObject playerObject, Packet packet) {
            Play(playerObject, packet, HeroController.instance.wallSlashPrefab, false, false, true);
        }
    }
}