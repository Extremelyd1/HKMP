using System.Collections;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

// TODO: perhaps play the screen shake also when our local player is close enough
namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the landing after a Descending Dark.
/// </summary>
internal class DescendingDarkLand : DamageAnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        MonoBehaviourUtil.Instance.StartCoroutine(PlayEffectInCoroutine(playerObject));
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }

    /// <summary>
    /// Plays the animation effect in a coroutine so we can wait during calls.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator PlayEffectInCoroutine(GameObject playerObject) {
        var spellControl = HeroController.instance.spellControl;

        // Get an audio source
        var audioObject = AudioUtil.GetAudioSourceObject(playerObject);
        var audioSource = audioObject.GetComponent<AudioSource>();

        // Find the land clip and play it
        var q2LandClip = (AudioClip) spellControl.GetFirstAction<AudioPlay>("Q2 Land").oneShotClip.Value;
        audioSource.PlayOneShot(q2LandClip);

        // Destroy the audio object after the clip is done
        Object.Destroy(audioObject, q2LandClip.length);

        var localPlayerSpells = spellControl.gameObject;
        var playerSpells = playerObject.FindGameObjectInChildren("Spells");

        // Destroy the existing Q Trail from the down effect
        Object.Destroy(playerSpells.FindGameObjectInChildren("Q Trail 2"));

        // Obtain the Q Slam prefab and instantiate it relative to the player object
        // This is the shockwave that happens when you impact the ground,
        // slightly larger than the Desolate Dive one
        var qSlamObject = localPlayerSpells.FindGameObjectInChildren("Q Slam 2");
        var quakeSlam = Object.Instantiate(
            qSlamObject,
            playerSpells.transform
        );
        quakeSlam.SetActive(true);
        quakeSlam.layer = 22;

        // If PvP is enabled add a DamageHero component to both hitbox sides
        var damage = ServerSettings.DescendingDarkDamage;

        if (ServerSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
            quakeSlam.FindGameObjectInChildren("Hit L").AddComponent<DamageHero>().damageDealt = damage;
            quakeSlam.FindGameObjectInChildren("Hit R").AddComponent<DamageHero>().damageDealt = damage;
        }

        // The FSM has a Wait action of 0.75 as a fallback for when the animationTrigger is not called.
        // It should be called at the 8th frame in the animation, which at 20 fps means 8/20 = 0.4s
        yield return new WaitForSeconds(0.4f);

        // Obtain the Q Pillar prefab and instantiate it relative to the player object
        // This is the void-looking column when you impact the ground
        var qPillarObject = localPlayerSpells.FindGameObjectInChildren("Q Pillar");
        var quakePillar = Object.Instantiate(
            qPillarObject,
            playerSpells.transform
        );
        quakePillar.SetActive(true);

        // Obtain the Q Mega prefab and instantiate it relative to the player object
        // This is the void tornado like effect around the knight when you impact the ground
        var qMegaObject = localPlayerSpells.FindGameObjectInChildren("Q Mega");
        var qMega = Object.Instantiate(
            qMegaObject,
            playerSpells.transform
        );
        qMega.SetActive(true);
        // Play the Q Mega animation from the first frame
        qMega.GetComponent<tk2dSpriteAnimator>().PlayFromFrame(0);

        // Enable the correct layer
        var qMegaHitL = qMega.FindGameObjectInChildren("Hit L");
        qMegaHitL.layer = 22;
        var qMegaHitR = qMega.FindGameObjectInChildren("Hit R");
        qMegaHitR.layer = 22;

        if (ServerSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
            qMegaHitL.AddComponent<DamageHero>().damageDealt = damage;
            qMegaHitR.AddComponent<DamageHero>().damageDealt = damage;
        }

        // Wait a second
        yield return new WaitForSeconds(1.0f);

        // And then destroy the remaining objects from the effect
        Object.Destroy(quakeSlam);
        Object.Destroy(quakePillar);
        Object.Destroy(qMega);
    }
}
