using UnityEngine;

namespace HKMP.Animation.Effects {
    public class ShadowDashSharpDown : DashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            Play(playerObject, effectInfo, true, true, true);
        }
    }
}