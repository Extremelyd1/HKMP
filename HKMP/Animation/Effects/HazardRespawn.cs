using UnityEngine;

namespace Hkmp.Animation.Effects {
    /// <summary>
    /// Animation effect class for the hazard respawn.
    /// </summary>
    internal class HazardRespawn : AnimationEffect {
        /// <inheritdoc/>
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // We only have to make the player visible again
            playerObject.SetActive(true);

            // TODO: perhaps implement the sprite flash
        }

        /// <inheritdoc/>
        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}