using System;
using System.Collections;
using System.Collections.Generic;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

// TODO: (dung)flukes are still client sided, perhaps find a efficient way to sync them?
namespace Hkmp.Animation.Effects {
    public abstract class FireballBase : DamageAnimationEffect {
        public abstract override void Play(GameObject playerObject, bool[] effectInfo);

        protected void Play(
            GameObject playerObject,
            bool[] effectInfo,
            string fireballParentName,
            int blastIndex,
            int castFireballIndex,
            int castAudioIndex,
            int dungFlukeIndex,
            int dungFlukeAudioIndex,
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
            var fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>(fireballParentName, 3).gameObject
                .Value;
            var fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            var audioAction = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", castAudioIndex);
            var audioPlayerObj = audioAction.audioPlayer.Value;

            // Get the scale of the player, so we know which way they are facing
            var localScale = playerObject.transform.localScale;
            var facingRight = localScale.x < 0;

            // First create the blast that appears in front of the knight
            if (blastIndex == 0) {
                // Take the blast object from the Cast Right state
                var blastObject = fireballCast.GetAction<CreateObject>("Cast Right", 3);

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
                castClip = (AudioClip) fireballCast.GetAction<AudioPlayerOneShotSingle>("Fluke R", 0).audioClip.Value;
                if (hasDefenderCrestCharm) {
                    var dungFlukeObj = fireballCast.GetAction<SpawnObjectFromGlobalPool>("Dung R", dungFlukeIndex)
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

                    var shamanStoneModifier = hasShamanStoneCharm ? 1.1f : 1.0f;

                    // Flip the dung fluke based on which direction the player is facing
                    // Also increase scale if we have Shaman Stone
                    var dungScale = dungFluke.transform.localScale;
                    dungFluke.transform.localScale = new Vector3(
                        (facingRight ? 1f : -1f) * shamanStoneModifier,
                        dungScale.y * shamanStoneModifier,
                        dungScale.z
                    );

                    if (GameSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
                        dungFluke.AddComponent<DamageHero>().damageDealt = damage;
                    }

                    // Start a coroutine, because we need to do some waiting in here
                    MonoBehaviourUtil.Instance.StartCoroutine(StartDungFluke(dungFluke, dungFlukeAudioIndex));

                    Object.Destroy(dungFluke.FindGameObjectInChildren("Damager"));
                } else {
                    MonoBehaviourUtil.Instance.StartCoroutine(StartFluke(fireballCast, playerSpells, facingRight,
                        damage));
                }
            } else {
                // We already had a variable for the actual fireball state containing the correct audio clip
                castClip = (AudioClip) audioAction.audioClip.Value;

                // Get the prefab and instantiate it
                var fireballObject = fireballCast.GetAction<SpawnObjectFromGlobalPool>("Cast Right", castFireballIndex)
                    .gameObject.Value;
                var fireball = Object.Instantiate(
                    fireballObject,
                    playerSpells.transform.position +
                    new Vector3(facingRight ? 1.168312f : -1.168312f, -0.5427618f, -0.002f),
                    Quaternion.identity
                );
                fireball.SetActive(true);

                // We add a fireball component that deals with spawning the moving fireball
                var fireballComponent = fireball.AddComponent<Fireball>();
                fireballComponent.xDir = -playerObject.transform.localScale.x;

                // Pass the relevant data to the fireball component
                fireballComponent.hasShamanStoneCharm = hasShamanStoneCharm;
                fireballComponent.baseFireballSize = baseFireballSize;
                fireballComponent.noFireballFlip = noFireballFlip;
                fireballComponent.shouldDoDamage = GameSettings.IsPvpEnabled && ShouldDoDamage;
                fireballComponent.damage = damage;
            }

            // Play the audio clip corresponding to which variation we spawned
            var audioPlayer = audioPlayerObj.Spawn(playerObject.transform);
            audioPlayer.GetComponent<AudioSource>().PlayOneShot(castClip);
        }

        public override bool[] GetEffectInfo() {
            var playerData = PlayerData.instance;

            return new[] {
                playerData.GetBool(nameof(PlayerData.equippedCharm_11)), // Flukenest
                playerData.GetBool(nameof(PlayerData.equippedCharm_10)), // Defender's Crest
                playerData.GetBool(nameof(PlayerData.equippedCharm_19)) // Shaman Stone
            };
        }

        private IEnumerator StartFluke(PlayMakerFSM fireballCast, GameObject playerSpells, bool facingRight,
            int damage) {
            // Obtain the prefab and instantiate it for the fluke only variation
            var flukeObject = fireballCast.GetAction<FlingObjectsFromGlobalPool>("Flukes", 0).gameObject.Value;
            var fluke = Object.Instantiate(
                flukeObject,
                playerSpells.transform.position,
                Quaternion.identity
            );

            if (GameSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
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

            if (GameSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
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
            if (GameSettings.IsPvpEnabled) {
                // Remove the burst delegate
                On.SpellFluke.Burst -= burstDelegate;
            }
        }

        private IEnumerator StartDungFluke(GameObject dungFluke, int dungFlukeAudioIndex) {
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

            // Get the control FSM and the audio clip corresponding to the explosion of the dungFluke
            // We need it later
            var dungFlukeControl = dungFluke.LocateMyFSM("Control");
            var blowClip = (AudioClip) dungFlukeControl.GetAction<AudioPlayerOneShotSingle>("Blow", dungFlukeAudioIndex)
                .audioClip.Value;
            Object.Destroy(dungFlukeControl);

            // Set the FSM state to Collider On, so we can actually interact with it
            dungFlukeControl.SetState("Collider On");
            // Play the explosion audio clip
            audioSource.Stop();
            audioSource.PlayOneShot(blowClip);

            if (GameSettings.IsPvpEnabled && ShouldDoDamage) {
                dungCloud.AddComponent<DamageHero>();
            }

            // We can already destroy the fluke
            Object.Destroy(dungFluke);

            yield return new WaitForSeconds(3.0f);

            // After some time, we can also destroy the cloud
            Object.Destroy(dungCloud);
        }
    }

    public class Fireball : MonoBehaviour {
        public float xDir;
        public bool hasShamanStoneCharm;
        public float baseFireballSize;
        public bool noFireballFlip;
        public bool shouldDoDamage;
        public int damage;

        private const float FireballSpeed = 45;

        private tk2dSpriteAnimator _anim;
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
}