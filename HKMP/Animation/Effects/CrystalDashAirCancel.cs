using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class CrystalDashAirCancel : AnimationEffect {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Get remote player effects object and play the end animation for the crystal dash trail
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");

            // Play the end animation for the crystal dash trail if it exists
            var sdTrail = playerEffects.FindGameObjectInChildren("SD Trail");
            if (sdTrail != null) {
                sdTrail.GetComponent<tk2dSpriteAnimator>().Play("SD Trail End");
            }

            var audioSourceObject = AudioUtil.GetAudioSourceObject(playerObject);

            var superDashFsm = HeroController.instance.gameObject.LocateMyFSM("Superdash");

            var airCancelAction = superDashFsm.GetAction<AudioPlay>("Air Cancel", 0);
            
            audioSourceObject.GetComponent<AudioSource>().PlayOneShot((AudioClip) airCancelAction.oneShotClip.Value);
            
            var superDashAudio = playerObject.FindGameObjectInChildren("Superdash Audio");
            if (superDashAudio != null) {
                superDashAudio.GetComponent<AudioSource>().Stop();
            }
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}