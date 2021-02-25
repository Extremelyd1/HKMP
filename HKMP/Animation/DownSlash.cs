using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    public class DownSlash : SlashBase {
        public override void Play(GameObject playerObject, Packet packet) {
            Play(playerObject, packet, HeroController.instance.downSlashPrefab, true, false, false);
        }
    }
}