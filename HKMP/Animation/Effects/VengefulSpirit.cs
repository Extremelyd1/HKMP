using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class VengefulSpirit : FireballBase {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Call the base play method with the correct indices and state names
            // This looks arbitrary, but is based on the FSM state machine of the fireball
            Play(
                playerObject,
                packet,
                "Fireball 1",
                0,
                7,
                6,
                1,
                3,
                1.0f,
                true,
                GameSettings.VengefulSpiritDamage
            );
        }
    }
}