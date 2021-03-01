using HKMP.Networking.Packet.Custom;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class CrystalDashAirCancel : IAnimationEffect {
        public void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Get remote player effects object and play the end animation for the crystal dash trail
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");
            playerEffects.FindGameObjectInChildren("SD Trail").GetComponent<tk2dSpriteAnimator>().Play("SD Trail End");
        }

        public void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}