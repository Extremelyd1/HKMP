using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation.Effects {
    /**
     * The animation effect during the movement of going down from a Descending Dark.
     */
    public class DescendingDarkDown : QuakeDownBase {
        public override void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo) {
            // Call the play method with the correct Q Trail prefab name
            Play(playerObject, skin, effectInfo, "Q Trail 2");
        }
    }
}