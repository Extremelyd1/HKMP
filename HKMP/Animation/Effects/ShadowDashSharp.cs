using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class ShadowDashSharp : DashBase {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            Play(playerObject, packet, true, true, false);
        }
    }
}