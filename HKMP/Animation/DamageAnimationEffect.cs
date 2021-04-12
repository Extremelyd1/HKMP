using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation {
    public abstract class DamageAnimationEffect : AnimationEffect {
        protected bool ShouldDoDamage;
        
        public abstract override void Play(GameObject playerObject,clientSkin skin , bool[] effectInfo);

        public abstract override bool[] GetEffectInfo();

        public void SetShouldDoDamage(bool shouldDoDamage) {
            ShouldDoDamage = shouldDoDamage;
        }
    }
}