using UnityEngine;
using HKMP.ServerKnights;

namespace HKMP.Animation {
    public abstract class AnimationEffect : IAnimationEffect {
        protected Game.Settings.GameSettings GameSettings;

        public abstract void Play(GameObject playerObject, clientSkin skin, bool[] effectInfo);

        public abstract bool[] GetEffectInfo();

        public void SetGameSettings(Game.Settings.GameSettings gameSettings) {
            GameSettings = gameSettings;
        }
    }
}