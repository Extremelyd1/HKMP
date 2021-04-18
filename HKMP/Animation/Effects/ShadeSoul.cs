using UnityEngine;

namespace HKMP.Animation.Effects {
    public class ShadeSoul : FireballBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Call the base play method with the correct indices and state names
            // This looks arbitrary, but is based on the FSM state machine of the fireball
            Play(
                playerObject,
                effectInfo,
                "Fireball 2",
                1,
                4,
                3,
                0, 
                4,
                1.8f,
                false,
                GameSettings.ShadeSoulDamage
            );
        }
    }
}