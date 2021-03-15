using UnityEngine;

namespace HKMP.Animation.Effects {
    public class DashDown : DashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            Play(playerObject, effectInfo, false, false, true);
        }
    }
}