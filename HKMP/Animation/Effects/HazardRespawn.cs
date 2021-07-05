using UnityEngine;

namespace Hkmp.Animation.Effects {
    public class HazardRespawn : AnimationEffect {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // We only have to make the player visible again
            playerObject.SetActive(true);

            // TODO: perhaps implement the sprite flash
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}