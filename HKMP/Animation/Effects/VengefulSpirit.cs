using UnityEngine;

namespace Hkmp.Animation.Effects {
    public class VengefulSpirit : FireballBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Call the base play method with the correct indices and state names
            // This looks arbitrary, but is based on the FSM state machine of the fireball
            Play(
                playerObject,
                effectInfo,
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