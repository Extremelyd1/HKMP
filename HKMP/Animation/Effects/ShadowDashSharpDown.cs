using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class ShadowDashSharpDown : DashBase {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            Play(playerObject, packet, true, true, true);
        }
    }
}