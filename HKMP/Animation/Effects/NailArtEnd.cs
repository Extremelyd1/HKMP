using HKMP.Networking.Packet.Custom;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class NailArtEnd : AnimationEffect {
        public override void Play(GameObject playerObject, ClientPlayerAnimationUpdatePacket packet) {
            // Get the player attacks object, which is where the nail art objects are stored
            var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");

            // Make a list of all object names that need to be destroyed
            var toDestroy = new [] {
                "Nail Art Charge", 
                "Nail Art Charged", 
                "Nail Art Charged Flash"
            };

            // Loop over the names and destroy the object if it exists
            foreach (var objectName in toDestroy) {
                var artObject = playerAttacks.FindGameObjectInChildren(objectName);
                if (artObject != null) {
                    Object.Destroy(artObject);
                }
            }

            // Make a list of all audio objects that need to be destroyed
            var audioToStop = new[] {
                "Nail Art Charge Audio",
                "Nail Art Charged Audio"
            };

            // Loop over the names and destroy the audio object if it exists
            foreach (var audioName in audioToStop) {
                var audioObject = playerAttacks.FindGameObjectInChildren(audioName);
                if (audioObject != null) {
                    Object.Destroy(audioObject.GetComponent<AudioSource>());
                }
            }
        }

        public override void PreparePacket(ServerPlayerAnimationUpdatePacket packet) {
        }
    }
}