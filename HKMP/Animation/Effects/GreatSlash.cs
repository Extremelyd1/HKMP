using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the Great Slash ability.
/// </summary>
internal class GreatSlash : ParryableEffect {
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Cancel the nail art charge animation if it exists
        AnimationManager.NailArtEnd.Play(playerObject);

        // Obtain the Nail Arts FSM from the Hero Controller
        var nailArts = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");

        // Get an audio source relative to the player
        var audioObject = AudioUtil.GetAudioSourceObject(playerObject);
        var audioSource = audioObject.GetComponent<AudioSource>();

        // Get the audio clip of the Great Slash
        var greatSlashClip = (AudioClip) nailArts.GetFirstAction<AudioPlay>("G Slash").oneShotClip.Value;
        audioSource.PlayOneShot(greatSlashClip);

        Object.Destroy(audioObject, greatSlashClip.length);

        // Get the attacks gameObject from the player object
        var localPlayerAttacks = HeroController.instance.gameObject.FindGameObjectInChildren("Attacks");
        var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");

        // Get the prefab for the Great Slash and instantiate it relative to the remote player object
        var greatSlashObject = localPlayerAttacks.FindGameObjectInChildren("Great Slash");
        var greatSlash = Object.Instantiate(
            greatSlashObject,
            playerAttacks.transform
        );
        greatSlash.layer = 22;

        ChangeAttackTypeOfFsm(greatSlash);

        greatSlash.SetActive(true);

        // Set the newly instantiate collider to state Init, to reset it
        // in case the local player was already performing it
        greatSlash.LocateMyFSM("Control Collider").SetState("Init");

        var damage = ServerSettings.GreatSlashDamage;
        if (ServerSettings.IsPvpEnabled && ShouldDoDamage) {
            if (ServerSettings.AllowParries) {
                AddParryFsm(greatSlash);
            }

            if (damage != 0) {
                greatSlash.AddComponent<DamageHero>().damageDealt = damage;
            }
        }

        // Get the animator, figure out the duration of the animation and destroy the object accordingly afterwards
        var greatSlashAnimator = greatSlash.GetComponent<tk2dSpriteAnimator>();
        var greatSlashAnimationDuration = greatSlashAnimator.DefaultClip.frames.Length / greatSlashAnimator.ClipFps;
        Object.Destroy(greatSlash, greatSlashAnimationDuration);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
