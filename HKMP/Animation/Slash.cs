using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    public class Slash : SlashBase {
        public override void Play(GameObject playerObject, Packet packet) {
            Play(playerObject, packet, HeroController.instance.slashPrefab, false, false, false);
        }
    }
}