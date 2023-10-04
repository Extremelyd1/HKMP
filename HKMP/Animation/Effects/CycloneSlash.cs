using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Animation.Effects;

/// <summary>
/// Animation effect class for the Cyclone Slash ability.
/// </summary>
internal class CycloneSlash : ParryableEffect {
    /// <summary>
    /// The GameObject for block effect of 'tinking' nails against each other.
    /// Used as the effect when players are bouncing on the Cyclone Slash.
    /// </summary>
    private readonly GameObject _tinkBlockEffect;

    public CycloneSlash() {
        var cycloneTink = HkmpMod.PreloadedObjects["GG_Sly"]["Battle Scene/Sly Boss/Cyclone Tink"];
        _tinkBlockEffect = cycloneTink.GetComponent<TinkEffect>().blockEffect;
    }
    
    /// <inheritdoc/>
    public override void Play(GameObject playerObject, bool[] effectInfo) {
        // Cancel the nail art charge animation if it exists
        AnimationManager.NailArtEnd.Play(playerObject);

        // Obtain the Nail Arts FSM from the Hero Controller
        var nailArts = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");

        // Obtain the AudioSource from the AudioPlayerOneShotSingle action in the nail arts FSM
        var audioAction = nailArts.GetFirstAction<AudioPlayerOneShotSingle>("Play Audio");
        var audioPlayerObj = audioAction.audioPlayer.Value;
        var audioPlayer = audioPlayerObj.Spawn(playerObject.transform);
        var audioSource = audioPlayer.GetComponent<AudioSource>();

        // Get the audio clip of the Cyclone Slash
        var cycloneClip = (AudioClip) audioAction.audioClip.Value;
        audioSource.PlayOneShot(cycloneClip);

        // Get the attacks gameObject from the player object
        var localPlayerAttacks = HeroController.instance.gameObject.FindGameObjectInChildren("Attacks");
        var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");

        // Get the prefab for the Cyclone Slash and instantiate it relative to the remote player object
        var cycloneObj = localPlayerAttacks.FindGameObjectInChildren("Cyclone Slash");
        var cycloneSlash = Object.Instantiate(
            cycloneObj,
            playerAttacks.transform
        );
        cycloneSlash.layer = 22;

        var hitLComponent = cycloneSlash.FindGameObjectInChildren("Hit L");
        ChangeAttackTypeOfFsm(hitLComponent);

        var hitRComponent = cycloneSlash.FindGameObjectInChildren("Hit R");
        ChangeAttackTypeOfFsm(hitRComponent);

        cycloneSlash.SetActive(true);

        // Set a name, so we can reference it later when we need to destroy it
        cycloneSlash.name = "Cyclone Slash";

        // Set the state of the Cyclone Slash Control Collider to init, to reset it
        // in case the local player was already performing it
        cycloneSlash.LocateMyFSM("Control Collider").SetState("Init");

        var damage = ServerSettings.CycloneSlashDamage;
        if (ServerSettings.IsPvpEnabled) {
            var tinkL = hitLComponent.AddComponent<TinkEffect>();
            var tinkR = hitRComponent.AddComponent<TinkEffect>();
            tinkL.blockEffect = _tinkBlockEffect;
            tinkR.blockEffect = _tinkBlockEffect;
            
            if (ShouldDoDamage && damage != 0) {
                hitLComponent.AddComponent<DamageHero>().damageDealt = damage;
                hitRComponent.AddComponent<DamageHero>().damageDealt = damage;
            }
        }

        // As a failsafe, destroy the cyclone slash after 4 seconds
        Object.Destroy(cycloneSlash, 4.0f);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        return null;
    }
}
