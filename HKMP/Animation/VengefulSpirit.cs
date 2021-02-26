using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    public class VengefulSpirit : FireballBase {
        public override void Play(GameObject playerObject, Packet packet) {
            Play(
                playerObject,
                packet,
                "Fireball 1",
                7,
                6,
                1,
                3,
                1.0f,
                true
            );
        }
    }
}