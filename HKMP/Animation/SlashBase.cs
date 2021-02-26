using HKMP.Networking.Packet;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using SpawnObjectFromGlobalPool = HutongGames.PlayMaker.Actions.SpawnObjectFromGlobalPool;
using AudioPlayerOneShotSingle = HutongGames.PlayMaker.Actions.AudioPlayerOneShotSingle;

namespace HKMP.Animation {
    public abstract class SlashBase : IAnimationEffect {
        public abstract void Play(GameObject playerObject, Packet packet);
        
        public void PreparePacket(Packet packet) {
            var playerData = PlayerData.instance;
            // Write health values to the packet
            packet.Write(playerData.health == 1);
            packet.Write(playerData.health == playerData.maxHealth);
            
            // Write charm values to the packet
            packet.Write(playerData.equippedCharm_6); // Fury of the fallen
            packet.Write(playerData.equippedCharm_13); // Mark of pride
            packet.Write(playerData.equippedCharm_18); // Long nail
            packet.Write(playerData.equippedCharm_35); // Grubberfly's Elegy
        }

        public void Play(GameObject playerObject, Packet packet, GameObject prefab, bool down, bool up, bool wall) {
            // Read all needed information to do this effect from the packet
            var isOnOneHealth = packet.ReadBool();
            var isOnFullHealth = packet.ReadBool();
            var hasFuryCharm = packet.ReadBool();
            var hasMarkOfPrideCharm = packet.ReadBool();
            var hasLongNailCharm = packet.ReadBool();
            var hasGrubberflyElegyCharm = packet.ReadBool();

            var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");

            var slash = Object.Instantiate(prefab, playerAttacks.transform);
            slash.SetActive(true);

            var slashAudioSource = slash.GetComponent<AudioSource>();
            var slashClip = slashAudioSource.clip;
            Object.Destroy(slashAudioSource);
            
            var spellControl = HeroController.instance.spellControl;
            var fireballParent = spellControl.GetAction<SpawnObjectFromGlobalPool>("Fireball 2", 3).gameObject.Value;
            var fireballCast = fireballParent.LocateMyFSM("Fireball Cast");
            var audioPlayerObj = fireballCast.GetAction<AudioPlayerOneShotSingle>("Cast Right", 3).audioPlayer.Value;

            var audioPlayer = audioPlayerObj.Spawn(playerObject.transform);
            audioPlayer.GetComponent<AudioSource>().PlayOneShot(slashClip);

            var fury = hasFuryCharm && isOnOneHealth;

            var nailSlash = slash.GetComponent<NailSlash>();
            nailSlash.SetMantis(hasMarkOfPrideCharm);
            nailSlash.SetFury(fury);

            if (!wall) {
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

            nailSlash.StartSlash();

            // TODO: deal with PvP scenarios

            if (hasGrubberflyElegyCharm) {
                GameObject elegyBeamPrefab = null;

                if (down) {
                    if (isOnOneHealth && hasFuryCharm) {
                        elegyBeamPrefab = HeroController.instance.grubberFlyBeamPrefabD_fury;
                    } else if (isOnFullHealth) {
                        elegyBeamPrefab = HeroController.instance.grubberFlyBeamPrefabD;
                    }
                } else if (up) {
                    if (isOnOneHealth && hasFuryCharm) {
                        elegyBeamPrefab = HeroController.instance.grubberFlyBeamPrefabU_fury;
                    } else if (isOnFullHealth) {
                        elegyBeamPrefab = HeroController.instance.grubberFlyBeamPrefabU;
                    }
                } else {
                    var facingLeft = playerObject.transform.localScale.x > 0;

                    if (facingLeft && isOnOneHealth && hasFuryCharm) {
                        elegyBeamPrefab = HeroController.instance.grubberFlyBeamPrefabL_fury;
                    } else if (facingLeft && isOnFullHealth) {
                        elegyBeamPrefab = HeroController.instance.grubberFlyBeamPrefabL;
                    } else if (!facingLeft && isOnOneHealth && hasFuryCharm) {
                        elegyBeamPrefab = HeroController.instance.grubberFlyBeamPrefabR_fury;
                    } else if (!facingLeft && isOnFullHealth) {
                        elegyBeamPrefab = HeroController.instance.grubberFlyBeamPrefabR;
                    }
                }

                if (elegyBeamPrefab != null) {
                    var elegyBeam = Object.Instantiate(
                        elegyBeamPrefab,
                        playerObject.transform.position,
                        Quaternion.identity
                    );

                    elegyBeam.SetActive(true);
                    elegyBeam.layer = 22;

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

                    // TODO: deal with PvP scenarios
                }
            }
        }
    }
}