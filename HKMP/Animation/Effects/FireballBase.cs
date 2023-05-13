using System.Collections;
using System.Collections.Generic;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

// TODO: (dung)flukes are still client sided, perhaps find a efficient way to sync them?
namespace Hkmp.Animation.Effects;

/// <summary>
/// Abstract base class for animation effect of fireball-based animations (Vengeful Spirit, Shade Soul
/// and variations).
/// </summary>
internal abstract class FireballBase : DamageAnimationEffect {
    /// <inheritdoc/>
    public abstract override void Play(GameObject playerObject, bool[] effectInfo);

    /// <summary>
    /// Play the animation effect of the fireball with all necessary parameters.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    /// <param name="effectInfo">A boolean array containing effect info.</param>
    /// <param name="fireballParentName">State name of the fireball parent state.</param>
    /// <param name="blastIndex">Index of the blast action.</param>
    /// <param name="baseFireballSize">Float for the size of the base fireball.</param>
    /// <param name="noFireballFlip">Whether to not flip the fireball sprite.</param>
    /// <param name="damage">The damage this spell should do.</param>
    protected void Play(
        GameObject playerObject,
        bool[] effectInfo,
        string fireballParentName,
        int blastIndex,
        float baseFireballSize,
        bool noFireballFlip,
        int damage
    ) {
        // Read the necessary data to create this effect
        var hasFlukenestCharm = effectInfo[0];
        var hasDefenderCrestCharm = effectInfo[1];
        var hasShamanStoneCharm = effectInfo[2];

        // Obtain the remote player spells object
        var playerSpells = playerObject.FindGameObjectInChildren("Spells");

        // Gather a bunch of object from the FSM state machine by indexing them
        // according to the parameters given to this function
        // They are different depending on which level of the Fireball spell we need to create
        var spellControl = HeroController.instance.spellControl;
        var fireballParent = spellControl.GetFirstAction<SpawnObjectFromGlobalPool>(fireballParentName).gameObject
            .Value;
        var fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
        var audioAction = fireballCast.GetFirstAction<AudioPlayerOneShotSingle>("Cast Right");
        var audioPlayerObj = audioAction.audioPlayer.Value;

        // Get the scale of the player, so we know which way they are facing
        var localScale = playerObject.transform.localScale;
        var facingRight = localScale.x < 0;

        // First create the blast that appears in front of the knight
        if (blastIndex == 0) {
            // Take the blast object from the Cast Right state
            var blastObject = fireballCast.GetFirstAction<CreateObject>("Cast Right");

            // Modify its position based on the values in the FSM and whether the player is facing left or right
            var position = playerSpells.transform.position
                           + new Vector3(facingRight ? 1.3f : -1.3f, 0, 0);
            // Instantiate it at that position
            var blast = Object.Instantiate(
                blastObject.gameObject.Value,
                position,
                Quaternion.identity
            );
            // Flip it based on which direction the player is facing
            blast.transform.localScale = new Vector3(facingRight ? 2 : -2, 2, 0);
        } else {
            // Apparently there is a 'roque' fireball hidden in the FSM object
            var blastObject = fireballCast.gameObject.FindGameObjectInChildren("Fireball2 Blast");

            // Modify the position based on the direction the player is facing
            var position = playerSpells.transform.position
                           + new Vector3(facingRight ? 1f : -1f, 0, 0);
            // Instantiate it at that position
            var blast = Object.Instantiate(
                blastObject,
                position,
                Quaternion.identity
            );
            // Flip it based on player direction
            blast.transform.localScale = new Vector3(facingRight ? 3.0f : -3.0f, 3.0f, 1.0f);
        }

        // Store the audio clip, each variation (flukenest, flukenest+defender crest, normal)
        // has an audio clip to play
        AudioClip castClip;
        if (hasFlukenestCharm) {
            // The audio clip for a variation containing flukenest is
            // always the one in the "Fluke R" state of the FSM
            castClip = (AudioClip) fireballCast.GetFirstAction<AudioPlayerOneShotSingle>("Fluke R").audioClip.Value;
            if (hasDefenderCrestCharm) {
                var dungFlukeObj = fireballCast.GetFirstAction<SpawnObjectFromGlobalPool>("Dung R")
                    .gameObject.Value;
                // Instantiate the dungFluke object from the prefab obtained above
                // Also spawn it a bit above the player position, so it doesn't get stuck
                var dungFluke = Object.Instantiate(
                    dungFlukeObj,
                    playerSpells.transform.position + new Vector3(0, 0.5f, 0),
                    Quaternion.identity);
                dungFluke.SetActive(true);

                var dungFlukeRigidBody = dungFluke.GetComponent<Rigidbody2D>();

                // Mimic the FlingObject action in the FSM
                var randomSpeed = 15;
                var randomAngle = facingRight
                    ? Random.Range(30, 40)
                    : Random.Range(140, 150);

                dungFlukeRigidBody.velocity = new Vector2(
                    randomSpeed * Mathf.Cos(randomAngle * ((float) System.Math.PI / 180f)),
                    randomSpeed * Mathf.Sin(randomAngle * ((float) System.Math.PI / 180f))
                );

                // Set the angular velocity as in the FSM
                dungFlukeRigidBody.angularVelocity = facingRight ? -100 : 100;

                // Make sure the object is scaled according to which direction the player is facing
                dungFluke.transform.rotation = Quaternion.Euler(0, 0, 26 * -localScale.x);
                dungFluke.layer = 22;

                var shamanStoneModifier = hasShamanStoneCharm ? 1.1f : 1.0f;

                // Flip the dung fluke based on which direction the player is facing
                // Also increase scale if we have Shaman Stone
                var dungScale = dungFluke.transform.localScale;
                dungFluke.transform.localScale = new Vector3(
                    (facingRight ? 1f : -1f) * shamanStoneModifier,
                    dungScale.y * shamanStoneModifier,
                    dungScale.z
                );

                if (ServerSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
                    dungFluke.AddComponent<DamageHero>().damageDealt = damage;
                }

                // Start a coroutine, because we need to do some waiting in here
                MonoBehaviourUtil.Instance.StartCoroutine(StartDungFluke(dungFluke));

                Object.Destroy(dungFluke.FindGameObjectInChildren("Damager"));
            } else {
                MonoBehaviourUtil.Instance.StartCoroutine(StartFluke(fireballCast, playerSpells, facingRight,
                    damage));
            }
        } else {
            // We already had a variable for the actual fireball state containing the correct audio clip
            castClip = (AudioClip) audioAction.audioClip.Value;

            // Get the prefab and instantiate it
            var fireballObject = fireballCast.GetFirstAction<SpawnObjectFromGlobalPool>("Cast Right")
                .gameObject.Value;
            var fireball = Object.Instantiate(
                fireballObject,
                playerSpells.transform.position +
                new Vector3(facingRight ? 1.168312f : -1.168312f, -0.5427618f, -0.002f),
                Quaternion.identity
            );
            fireball.SetActive(true);
            fireball.layer = 22;

            // We add a fireball component that deals with spawning the moving fireball
            var fireballComponent = fireball.AddComponent<Fireball>();
            fireballComponent.xDir = -playerObject.transform.localScale.x;

            // Pass the relevant data to the fireball component
            fireballComponent.hasShamanStoneCharm = hasShamanStoneCharm;
            fireballComponent.baseFireballSize = baseFireballSize;
            fireballComponent.noFireballFlip = noFireballFlip;
            fireballComponent.shouldDoDamage = ServerSettings.IsPvpEnabled && ShouldDoDamage;
            fireballComponent.damage = damage;
        }

        // Play the audio clip corresponding to which variation we spawned
        var audioPlayer = audioPlayerObj.Spawn(playerObject.transform);
        audioPlayer.GetComponent<AudioSource>().PlayOneShot(castClip);
    }

    /// <inheritdoc/>
    public override bool[] GetEffectInfo() {
        var playerData = PlayerData.instance;

        return new[] {
            playerData.GetBool(nameof(PlayerData.equippedCharm_11)), // Flukenest
            playerData.GetBool(nameof(PlayerData.equippedCharm_10)), // Defender's Crest
            playerData.GetBool(nameof(PlayerData.equippedCharm_19)) // Shaman Stone
        };
    }

    /// <summary>
    /// Start the animation for the flukes from the fireball cast.
    /// </summary>
    /// <param name="fireballCast">The FSM for the fireball cast.</param>
    /// <param name="playerSpells">The GameObject representing the player spells object within the player.</param>
    /// <param name="facingRight">Whether the spell is cast facing right.</param>
    /// <param name="damage">The damage of the spell.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator StartFluke(PlayMakerFSM fireballCast, GameObject playerSpells, bool facingRight,
        int damage) {
        // Obtain the prefab and instantiate it for the fluke only variation
        var flukeObject = fireballCast.GetFirstAction<FlingObjectsFromGlobalPool>("Flukes").gameObject.Value;
        var fluke = Object.Instantiate(
            flukeObject,
            playerSpells.transform.position,
            Quaternion.identity
        );

        if (ServerSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
            fluke.AddComponent<DamageHero>().damageDealt = damage;
        }

        // Create a config of how to fling the individual flukes
        // based on the direction the player is facing
        // This is all from the FSM
        var config = new FlingUtils.Config {
            Prefab = fluke,
            AmountMin = 16,
            AmountMax = 16,
            AngleMin = facingRight ? 20 : 120,
            AngleMax = facingRight ? 60 : 160,
            SpeedMin = 14,
            SpeedMax = 22
        };

        // Spawn the flukes relative to the player object with the created config
        var spawnedFlukes = FlingUtils.SpawnAndFling(
            config,
            playerSpells.transform,
            Vector3.zero
        );

        On.SpellFluke.hook_Burst burstDelegate = null;

        if (ServerSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
            // Keep track of SpellFluke components that we spawned
            var spellFlukes = new List<SpellFluke>();
            foreach (var spawnedFluke in spawnedFlukes) {
                var spellFlukeComponent = spawnedFluke.GetComponent<SpellFluke>();
                spellFlukes.Add(spellFlukeComponent);
            }

            // Make a delegate that fires when the fluke bursts and disable the DamageHero component
            burstDelegate = (orig, self) => {
                orig(self);

                if (spellFlukes.Contains(self)) {
                    var damageHeroComponent = self.gameObject.GetComponent<DamageHero>();
                    damageHeroComponent.enabled = false;
                    damageHeroComponent.damageDealt = damage;
                }
            };

            // Assign the delegate
            On.SpellFluke.Burst += burstDelegate;
        }

        yield return new WaitForSeconds(5.0f);

        // As a backup, destroy all spawned flukes after a maximum of 4 seconds
        foreach (var spawnedFluke in spawnedFlukes) {
            Object.Destroy(spawnedFluke);
        }

        // If we added a delegate, we can now remove it again
        if (ServerSettings.IsPvpEnabled) {
            // Remove the burst delegate
            On.SpellFluke.Burst -= burstDelegate;
        }
    }

    /// <summary>
    /// Start the animation for the dung fluke.
    /// </summary>
    /// <param name="dungFluke">The dung fluke GameObject.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator StartDungFluke(GameObject dungFluke) {
        var spriteAnimator = dungFluke.GetComponent<tk2dSpriteAnimator>();
        var dungSpazAudioClip = dungFluke.GetComponent<AudioSource>().clip;

        // Play the animation for the dungFluke movement and the corresponding audio
        spriteAnimator.Play("Dung Air");

        // Create an audio relative to the dung fluke
        var audioSource = AudioUtil.GetAudioSourceObject(dungFluke).GetComponent<AudioSource>();
        audioSource.clip = dungSpazAudioClip;
        audioSource.Play();

        yield return new WaitForSeconds(1.0f);

        // Play the erratic movement animation just before it explodes
        spriteAnimator.Play("Dung Antic");
        dungFluke.FindGameObjectInChildren("Pt Antic").GetComponent<ParticleSystem>().Play();

        yield return new WaitForSeconds(1.0f);

        // Now we get the prefab and spawn the actual explosion cloud
        var dungCloudObject = dungFluke.FindGameObjectInChildren("Knight Dung Cloud");
        var dungCloud = Object.Instantiate(
            dungCloudObject,
            dungFluke.transform.position,
            Quaternion.identity
        );

        dungCloud.SetActive(true);
        dungCloud.layer = 22;

        Object.Destroy(dungCloud.GetComponent<DamageEffectTicker>());

        // Get the control FSM and the audio clip corresponding to the explosion of the dungFluke
        // We need it later
        var dungFlukeControl = dungFluke.LocateMyFSM("Control");
        var blowClip = (AudioClip) dungFlukeControl.GetFirstAction<AudioPlayerOneShotSingle>("Blow")
            .audioClip.Value;
        Object.Destroy(dungFlukeControl);

        // Set the FSM state to Collider On, so we can actually interact with it
        dungFlukeControl.SetState("Collider On");
        // Play the explosion audio clip
        audioSource.Stop();
        audioSource.PlayOneShot(blowClip);

        if (ServerSettings.IsPvpEnabled && ShouldDoDamage) {
            dungCloud.AddComponent<DamageHero>();
        }

        // We can already destroy the fluke
        Object.Destroy(dungFluke);

        yield return new WaitForSeconds(3.0f);

        // After some time, we can also destroy the cloud
        Object.Destroy(dungCloud);
    }
}

/// <summary>
/// MonoBehaviour for the fireball effect.
/// </summary>
internal class Fireball : MonoBehaviour {
    /// <summary>
    /// Constant float for the speed of the fireball.
    /// </summary>
    private const float FireballSpeed = 45;

    /// <summary>
    /// The x direction (either 1 or -1) of the fireball.
    /// </summary>
    public float xDir;

    /// <summary>
    /// Whether the caster has the Shaman Stone charm equipped.
    /// </summary>
    public bool hasShamanStoneCharm;

    /// <summary>
    /// The base size of the fireball.
    /// </summary>
    public float baseFireballSize;

    /// <summary>
    /// Whether to not flip the fireball.
    /// </summary>
    public bool noFireballFlip;

    /// <summary>
    /// Whether the fireball should do damage.
    /// </summary>
    public bool shouldDoDamage;

    /// <summary>
    /// The damage of the fireball.
    /// </summary>
    public int damage;

    /// <summary>
    /// Cached sprite animator for the fireball.
    /// </summary>
    private tk2dSpriteAnimator _anim;

    /// <summary>
    /// Cached 2D rigid body for the fireball.
    /// </summary>
    private Rigidbody2D _rb;

    private void Awake() {
        _anim = GetComponent<tk2dSpriteAnimator>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start() {
        // Start playing the animation from the first frame
        _anim.PlayFromFrame(0);
        // Based on which direction the knight is facing, we set the velocity
        _rb.velocity = Vector2.right * FireballSpeed * xDir;

        // If PvP is enabled, add a DamageHero component to the fireball
        if (shouldDoDamage && damage != 0) {
            gameObject.AddComponent<DamageHero>().damageDealt = damage;
        }

        // For some reason, the FSM in the level 1 fireball flips the object
        // manually more times than the level 2 fireball, so we skip the flip
        if (noFireballFlip) {
            xDir = 1;
        }

        // Upscale the fireball if we have shaman stone equipped
        if (hasShamanStoneCharm) {
            transform.localScale = new Vector3(xDir * baseFireballSize * 1.3f, baseFireballSize * 1.6f, 0);
        } else {
            transform.localScale = new Vector3(xDir * baseFireballSize, baseFireballSize, 0);
        }

        // Destroy it after some time
        Destroy(gameObject, 2);
    }
}
