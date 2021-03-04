using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class CrystalDashAirCancel : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Get remote player effects object and play the end animation for the crystal dash trail
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");
            playerEffects.FindGameObjectInChildren("SD Trail").GetComponent<tk2dSpriteAnimator>().Play("SD Trail End");
            
            var audioSourceObject = AudioUtil.GetAudioSourceObject(playerObject);

            var superDashFsm = HeroController.instance.gameObject.LocateMyFSM("Superdash");

            var airCancelAction = superDashFsm.GetAction<AudioPlay>("Air Cancel", 0);
            
            audioSourceObject.GetComponent<AudioSource>().PlayOneShot((AudioClip) airCancelAction.oneShotClip.Value);
            
            var superDashAudio = playerObject.FindGameObjectInChildren("Superdash Audio");
            superDashAudio.GetComponent<AudioSource>().Stop();
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}