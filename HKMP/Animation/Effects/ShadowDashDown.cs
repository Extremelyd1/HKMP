using UnityEngine;

namespace HKMP.Animation.Effects {
    public class ShadowDashDown : DashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            Play(playerObject, effectInfo, true, false, true);
        }
    }
}