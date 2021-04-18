using UnityEngine;

namespace HKMP.Game.Client.Skin {
    public class PlayerSkin {
        public Texture KnightTexture { get; }
        public Texture SprintTexture { get; }

        public PlayerSkin(Texture knightTexture, Texture sprintTexture) {
            KnightTexture = knightTexture;
            SprintTexture = sprintTexture;
        }
    }
}