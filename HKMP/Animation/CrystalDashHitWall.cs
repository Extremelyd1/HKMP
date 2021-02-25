using HKMP.Networking.Packet;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation {
    public class CrystalDashHitWall : IAnimationEffect {
        public void Play(GameObject playerObject, Packet packet) {
            var heroEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Effects");
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");
            
            playerEffects.FindGameObjectInChildren("SD Trail").GetComponent<tk2dSpriteAnimator>().Play("SD Trail End");
                    
            GameObject wallHitEffect = Object.Instantiate(heroEffects.FindGameObjectInChildren("Wall Hit Effect"), playerEffects.transform);

            wallHitEffect.LocateMyFSM("FSM").InsertMethod("Destroy", 1, () => Object.Destroy(wallHitEffect));
        }

        public void PreparePacket(Packet packet) {
        }
    }
}