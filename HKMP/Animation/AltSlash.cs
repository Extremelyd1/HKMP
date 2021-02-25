using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    public class AltSlash : SlashBase {
        public override void Play(GameObject playerObject, Packet packet) {
            Play(playerObject, packet, HeroController.instance.slashAltPrefab, false, false, false);
        }
    }
}