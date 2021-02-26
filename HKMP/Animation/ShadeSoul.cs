using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    public class ShadeSoul : FireballBase {
        public override void Play(GameObject playerObject, Packet packet) {
            Play(
                playerObject,
                packet,
                "Fireball 2",
                4,
                3,
                0, 
                4,
                1.8f,
                false
            );
        }
    }
}