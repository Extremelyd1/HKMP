using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    public class ShadowDashSharpDown : DashBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            Play(playerObject, skin, effectInfo, true, true, true);
        }
    }
}