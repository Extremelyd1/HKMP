using HKMP.Networking.Packet.Custom;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class GreatSlash : IAnimationEffect {
        public void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Obtain the Nail Arts FSM from the Hero Controller
            var nailArts = HeroController.instance.gameObject.LocateMyFSM("Nail Arts");
            
            // Obtain the AudioSource from the AudioPlayerOneShotSingle action in the nail arts FSM
            var audioAction = nailArts.GetAction<AudioPlayerOneShotSingle>("Play Audio", 0);
            var audioPlayerObj = audioAction.audioPlayer.Value;
            var audioPlayer = audioPlayerObj.Spawn(playerObject.transform);
            var audioSource = audioPlayer.GetComponent<AudioSource>();
            
            // Get the audio clip of the Great Slash
            var greatSlashClip = (AudioClip) nailArts.GetAction<AudioPlay>("G Slash", 0).oneShotClip.Value;
            audioSource.PlayOneShot(greatSlashClip);
                    
            // Get the attacks gameObject from the player object
            var localPlayerAttacks = HeroController.instance.gameObject.FindGameObjectInChildren("Attacks");
            var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");
            
            // Get the prefab for the Great Slash and instantiate it relative to the remote player object
            var greatSlashObject = localPlayerAttacks.FindGameObjectInChildren("Great Slash");
            var greatSlash = Object.Instantiate(
                greatSlashObject,
                playerAttacks.transform
            );
            greatSlash.SetActive(true);
            greatSlash.layer = 22;

            // Set the newly instantiate collider to state Init, to reset it
            // in case the local player was already performing it
            greatSlash.LocateMyFSM("Control Collider").SetState("Init");

            if (Game.GameSettings.ClientInstance.IsPvpEnabled) {
                // Instantiate the Hive Knight Slash 
                var greatSlashCollider = Object.Instantiate(
                    HKMP.PreloadedObjects["HiveKnightSlash"],
                    greatSlash.transform
                );
                greatSlashCollider.SetActive(true);
                greatSlashCollider.layer = 22;

                // Copy over the polygon collider points
                greatSlashCollider.GetComponent<PolygonCollider2D>().points =
                    greatSlash.GetComponent<PolygonCollider2D>().points;
            }
            
            // Get the animator, figure out the duration of the animation and destroy the object accordingly afterwards
            var greatSlashAnimator = greatSlash.GetComponent<tk2dSpriteAnimator>();
            var greatSlashAnimationDuration = greatSlashAnimator.DefaultClip.frames.Length / greatSlashAnimator.ClipFps;
            Object.Destroy(greatSlash, greatSlashAnimationDuration);
        }

        public void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}