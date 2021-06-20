using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects {
    public class HowlingWraiths : ScreamBase {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            MonoBehaviourUtil.Instance.StartCoroutine(
                Play(playerObject, "Scream Antic1", "Scr Heads", GameSettings.HowlingWraithDamage)
            );
        }
    }
}