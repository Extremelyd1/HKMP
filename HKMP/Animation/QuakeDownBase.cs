using HKMP.Networking.Packet;
using ModCommon;
using UnityEngine;

namespace HKMP.Animation {
    /**
     * The animation effect during the movement of going down from a either Desolate Dive or Descending Dark.
     */
    public abstract class QuakeDownBase : IAnimationEffect {
        public abstract void Play(GameObject playerObject, Packet packet);
        
        public void Play(GameObject playerObject, Packet packet, string qTrailPrefabName) {
            // Obtain the local player spell control object
            var localPlayerSpells = HeroController.instance.spellControl.gameObject;

            // Instantiate the Q Flash Start from the prefab in the local player spell control object
            var qFlashStart = Object.Instantiate(
                localPlayerSpells.FindGameObjectInChildren("Q Flash Start"), 
                playerObject.transform
            );
            qFlashStart.SetActive(true);
            // And destroy it after a second
            Object.Destroy(qFlashStart, 1);
            
            // Instantiate the SD Sharp Flash from the prefab in the local player spell control object
            var sdSharpFlash = Object.Instantiate(
                localPlayerSpells.FindGameObjectInChildren("SD Sharp Flash"), 
                playerObject.transform
            );
            sdSharpFlash.SetActive(true);
            // And destroy it after a second
            Object.Destroy(sdSharpFlash, 1);
            
            var qTrail2 = Object.Instantiate(
                localPlayerSpells.FindGameObjectInChildren(qTrailPrefabName), 
                playerObject.transform
            );
            qTrail2.SetActive(true);
            // Assign a name so we reference it later, when we need to delete it
            qTrail2.name = "Q Trail 2 ";

            // Get the remote player spell object
            var playerSpells = playerObject.FindGameObjectInChildren("Spells");
            // Destroy the existing Q Charge from the antic
            Object.Destroy(playerSpells.FindGameObjectInChildren("Q Charge"));
        }

        public void PreparePacket(Packet packet) {
        }
    }
}