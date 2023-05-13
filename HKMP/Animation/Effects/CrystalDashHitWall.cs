using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

// TODO: perhaps play the screen shake also when our local player is close enough
namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for when a wall is hit with the Crystal Dash ability.
/// </summary>
internal class CrystalDashHitWall : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Get both the local player and remote player effects object
        var heroEffects = HeroController.instance.gameObject.FindGameObjectInChildren("Effects");
        var playerEffects = playerObject.FindGameObjectInChildren("Effects");

        // Play the end animation for the crystal dash trail if it exists
        var sdTrail = playerEffects.FindGameObjectInChildren("SD Trail");
        if (sdTrail != null) {
            sdTrail.GetComponent<tk2dSpriteAnimator>().Play("SD Trail End");
        }

        // Instantiate the wall hit effect and make sure to destroy it once the FSM is done
        var wallHitEffect = Object.Instantiate(
            heroEffects.FindGameObjectInChildren("Wall Hit Effect"),
            playerEffects.transform
        );
        wallHitEffect.LocateMyFSM("FSM").InsertMethod("Destroy", 1, () => Object.Destroy(wallHitEffect));

        var audioSourceObject = AudioUtil.GetAudioSourceObject(playerObject);

        var superDashFsm = HeroController.instance.gameObject.LocateMyFSM("Superdash");

        var wallHitAction = superDashFsm.GetFirstAction<AudioPlay>("Hit Wall");

        audioSourceObject.GetComponent<AudioSource>().PlayOneShot((AudioClip) wallHitAction.oneShotClip.Value);

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
