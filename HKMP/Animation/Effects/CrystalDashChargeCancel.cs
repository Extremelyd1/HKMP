using HKMP.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class CrystalDashChargeCancel : AnimationEffect {
        public override void Play(GameObject playerObject, bool[] effectInfo) {
            // Stop playing the charge audio
            var superDashAudio = playerObject.FindGameObjectInChildren("Superdash Charge Audio");
            if (superDashAudio != null) {
                superDashAudio.GetComponent<AudioSource>().Stop();
            }

            // We cancelled early, so we need to destroy the charge effect now already
            var playerEffects = playerObject.FindGameObjectInChildren("Effects");
            var chargeEffect = playerEffects.FindGameObjectInChildren("Charge Effect");

            Object.Destroy(chargeEffect);
            
            // Make sure that the coroutine of the crystal dash charge does not continue
            playerObject.GetComponent<CoroutineCancelComponent>().CancelCoroutine("Crystal Dash Charge");
        }

        public override bool[] GetEffectInfo() {
            return null;
        }
    }
}