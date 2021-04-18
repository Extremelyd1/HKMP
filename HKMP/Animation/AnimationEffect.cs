using UnityEngine;

namespace HKMP.Animation {
    public abstract class AnimationEffect : IAnimationEffect {
        protected Game.Settings.GameSettings GameSettings;

        public abstract void Play(GameObject playerObject, bool[] effectInfo);

        public abstract bool[] GetEffectInfo();

        public void SetGameSettings(Game.Settings.GameSettings gameSettings) {
            GameSettings = gameSettings;
        }
    }
}