using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    public class ShadowDash : DashBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            Play(playerObject, skin, effectInfo, true, false, false);
        }
    }
}