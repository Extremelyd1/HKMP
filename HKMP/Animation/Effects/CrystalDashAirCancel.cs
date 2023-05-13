using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for cancelling the Crystal Dash in mid-air.
/// </summary>
internal class CrystalDashAirCancel : AnimationEffect {
    /// <inheritdoc/>
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

        var airCancelAction = superDashFsm.GetFirstAction<AudioPlay>("Air Cancel");

        audioSourceObject.GetComponent<AudioSource>().PlayOneShot((AudioClip) airCancelAction.oneShotClip.Value);

        var superDashAudio = playerObject.FindGameObjectInChildren("Superdash Audio");
        if (superDashAudio != null) {
            superDashAudio.GetComponent<AudioSource>().Stop();
        }
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
