using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect for both Desolate Dive and Descending Dark abilities.
/// </summary>
internal class DiveAntic : AnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Get the spell control object from the local player object
        var localSpellControl = HeroController.instance.spellControl;

        // Get the AudioPlay action from the Quake Antic state
        var quakeAnticAudioPlay = localSpellControl.GetFirstAction<AudioPlay>("Quake Antic");

        var audioObject = AudioUtil.GetAudioSourceObject(playerObject);
        var audioSource = audioObject.GetComponent<AudioSource>();

        // Lastly, we get the clip we need to play
        var quakeAnticClip = (AudioClip) quakeAnticAudioPlay.oneShotClip.Value;
        // Now we can play the clip
        audioSource.PlayOneShot(quakeAnticClip);

        // Destroy the audio object after the clip is done
        Object.Destroy(audioObject, quakeAnticClip.length);

        // Get the remote player spell control object, to which we can assign the effect
        var playerSpellControl = playerObject.FindGameObjectInChildren("Spells");

        // Instantiate the Q Charge object from the prefab in the local spell control
        // Instantiate it relative to the remote player position
        var qCharge = Object.Instantiate(
            localSpellControl.gameObject.FindGameObjectInChildren("Q Charge"),
            playerSpellControl.transform
        );
        qCharge.SetActive(true);
        // Set the name, so we can reference it later, when we need to destroy it
        qCharge.name = "Q Charge";

        // Start the animation at the first frame
        qCharge.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(0);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
