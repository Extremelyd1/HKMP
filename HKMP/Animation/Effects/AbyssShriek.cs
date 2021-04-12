using HKMP.Util;
using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    public class AbyssShriek : ScreamBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            MonoBehaviourUtil.Instance.StartCoroutine(
                Play(playerObject, skin, "Scream Antic2", "Scr Heads 2", GameSettings.AbyssShriekDamage)
            );
        }
    }
}