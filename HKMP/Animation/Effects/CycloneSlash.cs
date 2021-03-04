using HKMP.Networking.Packet.Custom;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class CycloneSlash : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Obtain the Nail Arts FSM from the Hero Controller
            var nailArts = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");
            
            // Obtain the AudioSource from the AudioPlayerOneShotSingle action in the nail arts FSM
            var audioAction = nailArts.GetAction<AudioPlayerOneShotSingle>("Play Audio", 0);
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
            cycloneSlash.SetActive(true);
            cycloneSlash.layer = 22;
            // Set a name, so we can reference it later when we need to destroy it
            cycloneSlash.name = "Cyclone Slash";

            // Set the state of the Cyclone Slash Control Collider to init, to reset it
            // in case the local player was already performing it
            cycloneSlash.LocateMyFSM("Control Collider").SetState("Init");
            
            if (GameSettings.IsPvpEnabled) {
                var hitSides = new[] {
                    cycloneSlash.FindGameObjectInChildren("Hit L"),
                    cycloneSlash.FindGameObjectInChildren("Hit R")
                };

                foreach (var hitSide in hitSides) {
                    // We instantiate the Hive Knight Slash for the parry effect
                    var cycloneHitCollider = Object.Instantiate(
                        HKMP.PreloadedObjects["HiveKnightSlash"],
                        hitSide.transform
                    );
                    cycloneHitCollider.SetActive(true);
                    cycloneHitCollider.layer = 22;

                    // Get the polygon collider of the original and copy over the points
                    cycloneHitCollider.AddComponent<PolygonCollider2D>().points =
                        hitSide.GetComponent<PolygonCollider2D>().points;
                }
            }
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}