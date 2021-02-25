using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    public class UpSlash : SlashBase {
        public override void Play(GameObject playerObject, Packet packet) {
            Play(playerObject, packet, HeroController.instance.upSlashPrefab, false, true, false);
        }
    }
}