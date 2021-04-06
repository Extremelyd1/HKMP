using HKMP.Networking.Packet.Custom;
using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    public class CrystalDashWallCharge : CrystalDashChargeBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            Play(playerObject, skin, "Wall Charge", 16);
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}