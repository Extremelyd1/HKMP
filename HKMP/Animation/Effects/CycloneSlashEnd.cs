using HKMP.Networking.Packet;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation.Effects {
    public class CycloneSlashEnd : IAnimationEffect {
        public void Play(GameObject playerObject, Packet packet) {
            // Get the remote player attacks object
            var playerAttacks = playerObject.FindGameObjectInChildren("Attacks");
            // Find the object in the children of the attacks object
            var cycloneObject = playerAttacks.FindGameObjectInChildren("Cyclone Slash");
            if (cycloneObject != null) {
                // Destroy the Cyclone Slash object
                Object.Destroy(cycloneObject);
            }
        }

        public void PreparePacket(Packet packet) {
        }
    }
}