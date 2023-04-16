using System.Collections;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

// TODO: perhaps play the screen shake also when our local player is close enough
namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the landing of the Desolate Dive.
/// </summary>
internal class DesolateDiveLand : DamageAnimationEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        MonoBehaviourUtil.Instance.StartCoroutine(PlayEffectInCoroutine(playerObject));
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }

    /// <summary>
    /// Plays the animation effect in a coroutine.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator PlayEffectInCoroutine(GameObject playerObject) {
        var spellControl = HeroController.instance.spellControl;

        // Get an audio source
        var audioObject = AudioUtil.GetAudioSourceObject(playerObject);
        var audioSource = audioObject.GetComponent<AudioSource>();

        // Find the land clip and play it
        var qLandClip = (AudioClip) spellControl.GetFirstAction<AudioPlay>("Quake1 Land").oneShotClip.Value;
        audioSource.PlayOneShot(qLandClip);

        // Destroy the audio object after the clip is done
        Object.Destroy(audioObject, qLandClip.length);

        var localPlayerSpells = spellControl.gameObject;
        var playerSpells = playerObject.FindGameObjectInChildren("Spells");

        // Destroy the existing Q Trail from the down effect
        Object.Destroy(playerSpells.FindGameObjectInChildren("Q Trail"));

        // Obtain the Q Slam prefab and instantiate it relative to the player object
        // This is the shockwave that happens when you impact the ground
        var qSlamObject = localPlayerSpells.FindGameObjectInChildren("Q Slam");
        var quakeSlam = Object.Instantiate(
            qSlamObject,
            playerSpells.transform
        );
        quakeSlam.SetActive(true);
        quakeSlam.layer = 22;

        // If PvP is enabled add a DamageHero component to both hitbox sides
        var damage = ServerSettings.DesolateDiveDamage;

        if (ServerSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
            quakeSlam.FindGameObjectInChildren("Hit L").AddComponent<DamageHero>().damageDealt = damage;
            quakeSlam.FindGameObjectInChildren("Hit R").AddComponent<DamageHero>().damageDealt = damage;
        }

        // Obtain the Q1 Pillar prefab and instantiate it relative to the player object
        // This is the curvy pillar that comes from the sky once you impact the ground
        var qPillarObj = localPlayerSpells.FindGameObjectInChildren("Q1 Pillar");
        var quakePillar = Object.Instantiate(
            qPillarObj,
            playerSpells.transform
        );
        quakePillar.SetActive(true);

        // Wait a second
        yield return new WaitForSeconds(1.0f);
        //
        // // And then destroy the remaining objects from the effect
        Object.Destroy(quakeSlam);
        Object.Destroy(quakePillar);
    }
}
