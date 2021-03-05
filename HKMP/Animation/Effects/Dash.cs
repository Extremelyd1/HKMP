using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class Dash : DashBase {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            Play(playerObject, packet, false, false, false);
        }
    }
}