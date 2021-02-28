using HKMP.Networking.Packet;
using HKMP.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class HowlingWraiths : ScreamBase {
        public override void Play(GameObject playerObject, Packet packet) {
            MonoBehaviourUtil.Instance.StartCoroutine(
                Play(playerObject, packet, "Scream Antic1", "Scr Heads")
            );
        }
    }
}