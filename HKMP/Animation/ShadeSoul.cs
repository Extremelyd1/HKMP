using HKMP.Networking.Packet;
using UnityEngine;

namespace HKMP.Animation {
    public class ShadeSoul : FireballBase {
        public override void Play(GameObject playerObject, Packet packet) {
            // Call the base play method with the correct indices and state names
            // This looks arbitrary, but is based on the FSM state machine of the fireball
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