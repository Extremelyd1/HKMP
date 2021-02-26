using HKMP.Networking.Packet;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation {
    public class CrystalDashHitWall : IAnimationEffect {
        public void Play(GameObject playerObject, Packet packet) {
            // Get both the local player and remote player effects object
            var heroEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Effects");
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");
            
            // Play the end animation for the crystal dash trail
            playerEffects.FindGameObjectInChildren("SD Trail").GetComponent<tk2dSpriteAnimator>().Play("SD Trail End");
            
            // Instantiate the wall hit effect and make sure to destroy it once the FSM is done
            var wallHitEffect = Object.Instantiate(heroEffects.FindGameObjectInChildren("Wall Hit Effect"), playerEffects.transform);
            wallHitEffect.LocateMyFSM("FSM").InsertMethod("Destroy", 1, () => Object.Destroy(wallHitEffect));
        }

        public void PreparePacket(Packet packet) {
        }
    }
}