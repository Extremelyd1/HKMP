using UnityEngine;

namespace Hkmp.Animation.Effects {
    public class Dash : DashBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            Play(playerObject, effectInfo, false, false, false);
        }
    }
}