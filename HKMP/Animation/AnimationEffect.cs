using HKMP.Networking.Packet.Custom;
using UnityEngine;

namespace HKMP.Animation {
    public abstract class AnimationEffect : IAnimationEffect {
        protected Game.Settings.GameSettings GameSettings;

        public abstract void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet);

        public abstract void PreparePacket(ServerPlayerAnimationUpdatePacket packet);

        public void SetGameSettings(Game.Settings.GameSettings gameSettings) {
            GameSettings = gameSettings;
        }
    }
}