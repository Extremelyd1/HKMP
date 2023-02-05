using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for cancelling the charge of the Crystal Dash ability.
/// </summary>
internal class CrystalDashChargeCancel : AnimationEffect {
    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
