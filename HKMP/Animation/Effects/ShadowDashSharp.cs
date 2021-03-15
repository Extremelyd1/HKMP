using UnityEngine;

namespace HKMP.Animation.Effects {
    public class ShadowDashSharp : DashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            Play(playerObject, effectInfo, true, true, false);
        }
    }
}