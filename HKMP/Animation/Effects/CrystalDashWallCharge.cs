using UnityEngine;

namespace Hkmp.Animation.Effects {
    public class CrystalDashWallCharge : CrystalDashChargeBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            Play(playerObject, "Wall Charge", 16);
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}