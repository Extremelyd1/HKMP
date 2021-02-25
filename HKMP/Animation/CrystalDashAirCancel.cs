using HKMP.Networking.Packet;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation {
    public class CrystalDashAirCancel : AnimationEffect {
        public void Play(GameObject playerObject, Packet packet) {
            var heroEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Effects");
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");
            
            var sdBreak = Object.Instantiate(
                heroEffects.FindGameObjectInChildren("SD Break"), 
                playerEffects.transform);
            Object.Destroy(sdBreak, 0.54f);
            
            playerEffects.FindGameObjectInChildren("SD Trail").GetComponent<tk2dSpriteAnimator>().Play("SD Trail End");
        }
    }
}