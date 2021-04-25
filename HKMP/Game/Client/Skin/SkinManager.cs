using System.Collections.Generic;
using UnityEngine;

namespace HKMP.Game.Client.Skin {
    /**
     * Class that manages skins for player objects.
     */
    public class SkinManager {
        // Dictionary mapping skin IDs to PlayerSkin objects that store all relevant textures
        private readonly Dictionary<byte, PlayerSkin> _playerSkins;

        // The fallback skin to use
        private PlayerSkin _defaultPlayerSkin;

        public SkinManager() {
            _playerSkins = new Dictionary<byte, PlayerSkin>();

            new SkinLoader().LoadAllSkins(ref _playerSkins);

            // Only when the local player object is created can we retrieve the default materials from it,
            // so we register this on HeroController Start
            On.HeroController.Start += (orig, self) => {
                orig(self);

                // If we haven't saved the default skin already
                if (_defaultPlayerSkin == null) {
                    Logger.Get().Info(this, "Storing default player skin");
                    StoreDefaultPlayerSkin(self);
                }
            };
        }

        /**
         * Update the player skin on the given player object with the given skin ID.
         * An ID of 0 or an ID that doesn't have a valid skin loaded, will result in the default skin being loaded.
         */
        public void UpdatePlayerSkin(GameObject playerObject, byte skinId) {
            var playerSkin = _defaultPlayerSkin;

            if (skinId != 0) {
                if (!_playerSkins.TryGetValue(skinId, out playerSkin)) {
                    Logger.Get().Warn(this, $"Tried to update skin with ID: {skinId}, but there was no such skin loaded");

                    playerSkin = _defaultPlayerSkin;
                }
            }

            // SetTextureInMaterialBlock(playerObject, playerSkin.KnightTexture);
            var spriteAnimator = playerObject.GetComponent<tk2dSpriteAnimator>();
            if (spriteAnimator == null) {
                Logger.Get().Warn(this, "Tried to update player skin, but SpriteAnimator is null");
                return;
            }
            
            spriteAnimator
                .GetClipByName("Idle")
                .frames[0]
                .spriteCollection
                .spriteDefinitions[0]
                .material
                .mainTexture = playerSkin.KnightTexture;
            spriteAnimator
                .GetClipByName("Sprint")
                .frames[0]
                .spriteCollection
                .spriteDefinitions[0]
                .material
                .mainTexture = playerSkin.SprintTexture;
        }

        /**
         * Updates the local player skin to the skin with the given ID.
         */
        public void UpdateLocalPlayerSkin(byte skinId) {
            var heroController = HeroController.instance;
            if (heroController == null) {
                Logger.Get().Warn(this, "Tried to update local player skin, but HeroController instance is null");
                return;
            }
            
            var localPlayerObject = heroController.gameObject;
            if (localPlayerObject == null) {
                Logger.Get().Warn(this, "Tried to update local player skin, but HeroController object is null");
                return;
            }

            UpdatePlayerSkin(localPlayerObject, skinId);
        }

        public void ResetPlayerSkin(GameObject playerObject) {
            UpdatePlayerSkin(playerObject, 0);
        }

        /**
         * Resets the local player skin to the default skin.
         */
        public void ResetLocalPlayerSkin() {
            UpdateLocalPlayerSkin(0);
        }
        
        private void StoreDefaultPlayerSkin(HeroController heroController) {
            var localPlayerObject = heroController.gameObject;
            var spriteAnimator = localPlayerObject.GetComponent<tk2dSpriteAnimator>();

            if (spriteAnimator == null) {
                return;
            }

            // Get the knight and sprint texture from the sprite definitions of the respective animation clips
            // in the sprite animator of the local HeroController object
            var knightTexture = spriteAnimator
                .GetClipByName("Idle")?
                .frames[0]?
                .spriteCollection
                .spriteDefinitions[0]?
                .material
                .mainTexture as Texture2D;
            var sprintTexture = spriteAnimator
                .GetClipByName("Sprint")?
                .frames[0]?
                .spriteCollection
                .spriteDefinitions[0]?
                .material
                .mainTexture as Texture2D;

            if (knightTexture == null) {
                Logger.Get().Warn(this, "Tried to store default player skin, but knight texture was null");
                return;
            }
            if (sprintTexture == null) {
                Logger.Get().Warn(this, "Tried to store default player skin, but sprint texture was null");
                return;
            }

            _defaultPlayerSkin = new PlayerSkin(knightTexture, sprintTexture);
        }
    }
}