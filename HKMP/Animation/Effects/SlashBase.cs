using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public abstract class SlashBase : DamageAnimationEffect {
        public abstract override void Play(GameObject playerObject, bool[] effectInfo);

        public override bool[] GetEffectInfo() {
            var playerData = PlayerData.instance;

            return new[] {
                playerData.health == 1,
                playerData.health == playerData.maxHealth,
                playerData.equippedCharm_6, // Fury of the fallen
                playerData.equippedCharm_13, // Mark of pride
                playerData.equippedCharm_18, // Long nail
                playerData.equippedCharm_35 // Grubberfly's Elegy
            };
        }

        protected void Play(GameObject playerObject, bool[] effectInfo, GameObject prefab, bool down, bool up, bool wall) {
            // Read all needed information to do this effect from the packet
            var isOnOneHealth = effectInfo[0];
            var isOnFullHealth = effectInfo[1];
            var hasFuryCharm = effectInfo[2];
            var hasMarkOfPrideCharm = effectInfo[3];
            var hasLongNailCharm = effectInfo[4];
            var hasGrubberflyElegyCharm = effectInfo[5];

            // Get the attacks gameObject from the player object
            var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");

            // Instantiate the slash gameObject from the given prefab
            // and use the attack gameObject as transform reference
            var slash = Object.Instantiate(prefab, playerAttacks.transform);
            slash.SetActive(true);

            // Get the slash audio source and its clip
            var slashAudioSource = slash.GetComponent<AudioSource>();
            // Remove original audio source to prevent double audio
            Object.Destroy(slashAudioSource);
            var slashClip = slashAudioSource.clip;
            
            // Obtain the Nail Arts FSM from the Hero Controller
            var nailArts = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");
            
            // Obtain the AudioSource from the AudioPlayerOneShotSingle action in the nail arts FSM
            var audioAction = nailArts.GetAction<AudioPlayerOneShotSingle>("Play Audio", 0);
            var audioPlayerObj = audioAction.audioPlayer.Value;
            var audioPlayer = audioPlayerObj.Spawn(playerObject.transform);
            var audioSource = audioPlayer.GetComponent<AudioSource>();
            
            // Play the slash clip with this newly spawned AudioSource
            audioSource.PlayOneShot(slashClip);

            // Store a boolean indicating whether the Fury of the fallen effect is active
            var fury = hasFuryCharm && isOnOneHealth;

            // Get the NailSlash component and set its values
            // based on the charms and fury state we have
            var nailSlash = slash.GetComponent<NailSlash>();
            nailSlash.SetLongnail(hasLongNailCharm);
            nailSlash.SetMantis(hasMarkOfPrideCharm);
            nailSlash.SetFury(fury);

            // If it is a wall slash, there is no scaling to do
            if (!wall) {
                // Scale the nail slash based on Long nail and Mark of pride charms
                if (hasLongNailCharm) {
                    if (hasMarkOfPrideCharm) {
                        nailSlash.transform.localScale = new Vector3(nailSlash.scale.x * 1.4f, nailSlash.scale.y * 1.4f,
                            nailSlash.scale.z);
                    } else {
                        nailSlash.transform.localScale = new Vector3(nailSlash.scale.x * 1.25f,
                            nailSlash.scale.y * 1.25f,
                            nailSlash.scale.z);
                    }
                } else if (hasMarkOfPrideCharm) {
                    nailSlash.transform.localScale = new Vector3(nailSlash.scale.x * 1.15f, nailSlash.scale.y * 1.15f,
                        nailSlash.scale.z);
                }
            }

            // Finally start the slash animation
            nailSlash.StartSlash();

            var damage = GameSettings.NailDamage;
            if (GameSettings.IsPvpEnabled && ShouldDoDamage && damage != 0) {
                // TODO: make it possible to pogo on players
                
                // Instantiate the preloaded Hive Knight Slash, since it contains 
                // the nail clash tink FSM that is needed for the clash effect
                var slashCollider = Object.Instantiate(
                    HKMP.PreloadedObjects["HiveKnightSlash"],
                    slash.transform
                );
                slashCollider.SetActive(true);
                slashCollider.layer = 22;
                
                // Make sure that the FSM state is set to Initiate, otherwise it doesn't do anything
                var slashColliderFsm = slashCollider.GetComponent<PlayMakerFSM>();
                slashColliderFsm.SetState("Initiate");

                // Copy the polygon collider points from the slash to this new object
                slashCollider.GetComponent<PolygonCollider2D>().points = slash.GetComponent<PolygonCollider2D>().points;
                
                slashCollider.GetComponent<DamageHero>().damageDealt = damage;
            }
            
            // After the animation is finished, we can destroy the slash object
            var slashAnimator = slash.GetComponent<tk2dSpriteAnimator>();
            var animationDuration = slashAnimator.DefaultClip.Duration;
            
            Object.Destroy(slash, animationDuration);

            if (!hasGrubberflyElegyCharm
                || isOnOneHealth && !hasFuryCharm
                || !isOnFullHealth) {
                return;
            }

            GameObject elegyBeamPrefab;

            // Store a boolean indicating that we should take the fury variant of the beam prefab
            var furyVariant = isOnOneHealth;
            if (down) {
                elegyBeamPrefab = furyVariant
                    ? HeroController.instance.grubberFlyBeamPrefabD_fury
                    : HeroController.instance.grubberFlyBeamPrefabD;
            } else if (up) {
                elegyBeamPrefab = furyVariant
                    ? HeroController.instance.grubberFlyBeamPrefabU_fury
                    :HeroController.instance.grubberFlyBeamPrefabU;
            } else {
                var facingLeft = playerObject.transform.localScale.x > 0;

                if (facingLeft) {
                    elegyBeamPrefab = furyVariant
                        ? HeroController.instance.grubberFlyBeamPrefabL_fury
                        : HeroController.instance.grubberFlyBeamPrefabL;
                } else {
                    elegyBeamPrefab = furyVariant
                        ? HeroController.instance.grubberFlyBeamPrefabR_fury
                        : HeroController.instance.grubberFlyBeamPrefabR;
                }
            }
            
            // Instantiate the beam from the prefab with the playerObject position
            var elegyBeam = Object.Instantiate(
                elegyBeamPrefab,
                playerObject.transform.position,
                Quaternion.identity
            );

            elegyBeam.SetActive(true);
            elegyBeam.layer = 22;

            // Rotate the beam if it is an up or down slash
            var localScale = elegyBeam.transform.localScale;
            if (up || down) {
                elegyBeam.transform.localScale = new Vector3(
                    playerObject.transform.localScale.x,
                    localScale.y,
                    localScale.z
                );
                var z = 90;
                if (down && playerObject.transform.localScale.x < 0) {
                    z = -90;
                }

                if (up && playerObject.transform.localScale.x > 0) {
                    z = -90;
                }

                elegyBeam.transform.rotation = Quaternion.Euler(
                    0,
                    0,
                    z
                );
            }

            Object.Destroy(elegyBeam.LocateMyFSM("damages_enemy"));
            
            // If PvP is enabled, simply add a DamageHero component to the beam
            var elegyDamage = GameSettings.GrubberyFlyElegyDamage;
            if (GameSettings.IsPvpEnabled && ShouldDoDamage && elegyDamage != 0) {
                elegyBeam.AddComponent<DamageHero>().damageDealt = elegyDamage;
            }
            
            // We can destroy the elegy beam object after some time
            Object.Destroy(elegyBeam, 2.0f);
        }
    }
}