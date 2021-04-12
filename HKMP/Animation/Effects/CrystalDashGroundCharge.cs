using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    public class CrystalDashGroundCharge : CrystalDashChargeBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            Play(playerObject,  skin , "Ground Charge", 11);
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}