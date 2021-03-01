using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class HowlingWraiths : ScreamBase {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            MonoBehaviourUtil.Instance.StartCoroutine(
                Play(playerObject, packet, "Scream Antic1", "Scr Heads")
            );
        }
    }
}