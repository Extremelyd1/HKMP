using UnityEngine;

namespace Hkmp.Animation.Effects {
    public class CrystalDashGroundCharge : CrystalDashChargeBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            Play(playerObject, "Ground Charge", 11);
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}