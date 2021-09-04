using UnityEngine;

namespace Hkmp.Game.Client.Skin {
    public class PlayerSkin {
        
        public bool HasKnightTexture { get; private set; }
        public Texture KnightTexture { get; private set; }
        
        public bool HasSprintTexture { get; private set; }
        public Texture SprintTexture { get; private set; }

        public void SetKnightTexture(Texture knightTexture) {
            KnightTexture = knightTexture;
            HasKnightTexture = true;
        }

        public void SetSprintTexture(Texture sprintTexture) {
            SprintTexture = sprintTexture;
            HasSprintTexture = true;
        }
    }
}