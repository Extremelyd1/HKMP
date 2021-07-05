using UnityEngine;

namespace Hkmp.Animation {
    public abstract class DamageAnimationEffect : AnimationEffect {
        protected bool ShouldDoDamage;

        public abstract override void Play(GameObject playerObject, bool[] effectInfo);

        public abstract override bool[] GetEffectInfo();

        public void SetShouldDoDamage(bool shouldDoDamage) {
            ShouldDoDamage = shouldDoDamage;
        }
    }
}