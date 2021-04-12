using HKMP.Util;
using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    public class HowlingWraiths : ScreamBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            MonoBehaviourUtil.Instance.StartCoroutine(
                Play(playerObject, skin ,"Scream Antic1", "Scr Heads", GameSettings.HowlingWraithDamage)
            );
        }
    }
}