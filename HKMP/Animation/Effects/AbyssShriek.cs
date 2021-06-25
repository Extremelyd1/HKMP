using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects {
    public class AbyssShriek : ScreamBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            MonoBehaviourUtil.Instance.StartCoroutine(
                Play(playerObject, "Scream Antic2", "Scr Heads 2", GameSettings.AbyssShriekDamage)
            );
        }
    }
}