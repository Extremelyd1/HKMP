using HKMP.Networking.Packet;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation {
    public class CrystalDash : IAnimationEffect {
        public void Play(GameObject playerObject, Packet packet) {
            var heroEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Effects");
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");

            var sdBurst = Object.Instantiate(
                heroEffects.FindGameObjectInChildren("SD Burst"),
                playerEffects.transform
            );
            sdBurst.SetActive(true);

            sdBurst.LocateMyFSM("FSM").InsertMethod("Destroy", 1, () => { Object.Destroy(sdBurst); });

            var sdTrail = Object.Instantiate(
                heroEffects.FindGameObjectInChildren("SD Trail"),
                playerEffects.transform
            );
            sdTrail.SetActive(true);
            sdTrail.name = "SD Trail";

            var sdTrailFsm = sdTrail.LocateMyFSM("FSM");
            sdTrailFsm.SetState("Idle");
            sdTrailFsm.InsertMethod("Destroy", 1, () => { Object.Destroy(sdTrail); });

            sdTrail.GetComponent<MeshRenderer>().enabled = true;

            sdTrail.GetComponent<tk2dSpriteAnimator>().PlayFromFrame("SD Trail", 0);

            var sdBurstGlow = Object.Instantiate(
                heroEffects.FindGameObjectInChildren("SD Burst Glow"),
                playerEffects.transform
            );
            sdBurstGlow.SetActive(true);
        }

        public void PreparePacket(Packet packet) {
        }
    }
}