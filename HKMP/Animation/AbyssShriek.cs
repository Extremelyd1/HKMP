using HKMP.Networking.Packet;
using HKMP.Util;
using UnityEngine;

namespace HKMP.Animation {
    public class AbyssShriek : ScreamBase {
        public override void Play(GameObject playerObject, Packet packet) {
            CoroutineUtil.Instance.StartCoroutine(
                Play(playerObject, packet, "Scream Antic2", "Scr Heads 2")
            );
        }
    }
}