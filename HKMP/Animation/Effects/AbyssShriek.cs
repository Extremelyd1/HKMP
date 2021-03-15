using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class AbyssShriek : ScreamBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            MonoBehaviourUtil.Instance.StartCoroutine(
                Play(playerObject, "Scream Antic2", "Scr Heads 2", GameSettings.AbyssShriekDamage)
            );
        }
    }
}