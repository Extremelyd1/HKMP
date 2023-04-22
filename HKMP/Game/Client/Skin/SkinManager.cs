using System.Collections.Generic;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Skin;

/// <summary>
/// Class that manages skins for player objects.
/// </summary>
internal class SkinManager {
    /// <summary>
    /// Dictionary mapping skin IDs to PlayerSkin objects that store all relevant textures.
    /// </summary>
    private readonly Dictionary<byte, PlayerSkin> _playerSkins;

    /// <summary>
    /// The fallback skin to use.
    /// </summary>
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
                Logger.Debug("Storing default player skin");
                StoreDefaultPlayerSkin(self);
            }

            InitializeSpritesOnLocalPlayer(self.gameObject);
        };
    }

    /// <summary>
    /// This method loads the Sprint animation on the local player to ensure that whenever the animation
    /// library is copied for skin purposes, it doesn't lack the instantiations of that animation.
    ///
    /// Note: when expanding the skin system to more sprites, update this method as well.
    /// </summary>
    /// <param name="gameObject">The GameObject of the local player.</param>
    private void InitializeSpritesOnLocalPlayer(GameObject gameObject) {
        var spriteAnimator = gameObject.GetComponent<tk2dSpriteAnimator>();
        if (spriteAnimator == null) {
            Logger.Warn("Tried to initialize sprites on local player, but SpriteAnimator is null");
            return;
        }

        var firstSpriteFrame = spriteAnimator.GetClipByName("Sprint").frames[0];
        spriteAnimator.SetSprite(firstSpriteFrame.spriteCollection, firstSpriteFrame.spriteId);

        firstSpriteFrame = spriteAnimator.GetClipByName("Slug Idle").frames[0];
        spriteAnimator.SetSprite(firstSpriteFrame.spriteCollection, firstSpriteFrame.spriteId);

        Logger.Debug("Initialized sprites on local player");
    }

    /// <summary>
    /// Update the player skin on the given player object with the given skin ID. An ID of 0 or an ID
    /// that doesn't have a valid skin loaded, will result in the default skin being loaded.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player to update.</param>
    /// <param name="skinId">The ID of the skin to apply.</param>
    public void UpdatePlayerSkin(GameObject playerObject, byte skinId) {
        if (playerObject == null) {
            return;
        }

        var playerSkin = _defaultPlayerSkin;

        if (skinId != 0) {
            if (!_playerSkins.TryGetValue(skinId, out playerSkin)) {
                Logger.Warn($"Tried to update skin with ID: {skinId}, but there was no such skin loaded");

                playerSkin = _defaultPlayerSkin;
            }
        }

        // SetTextureInMaterialBlock(playerObject, playerSkin.KnightTexture);
        var spriteAnimator = playerObject.GetComponent<tk2dSpriteAnimator>();
        if (spriteAnimator == null) {
            Logger.Warn("Tried to update player skin, but SpriteAnimator is null");
            return;
        }

        if (playerSkin.HasKnightTexture) {
            spriteAnimator
                .GetClipByName("Idle")
                .frames[0]
                .spriteCollection
                .spriteDefinitions[0]
                .material
                .mainTexture = playerSkin.KnightTexture;
        }

        if (playerSkin.HasSprintTexture) {
            spriteAnimator
                .GetClipByName("Sprint")
                .frames[0]
                .spriteCollection
                .spriteDefinitions[0]
                .material
                .mainTexture = playerSkin.SprintTexture;
        }
    }

    /// <summary>
    /// Updates the local player skin to the skin with the given ID.
    /// </summary>
    /// <param name="skinId">The ID of the skin to apply.</param>
    public void UpdateLocalPlayerSkin(byte skinId) {
        var heroController = HeroController.instance;
        if (heroController == null) {
            Logger.Warn("Tried to update local player skin, but HeroController instance is null");
            return;
        }

        var localPlayerObject = heroController.gameObject;
        if (localPlayerObject == null) {
            Logger.Warn("Tried to update local player skin, but HeroController object is null");
            return;
        }

        UpdatePlayerSkin(localPlayerObject, skinId);
    }

    /// <summary>
    /// Reset the skin of the given player to the default skin.
    /// </summary>
    /// <param name="playerObject">The GameObject representing the player.</param>
    public void ResetPlayerSkin(GameObject playerObject) {
        UpdatePlayerSkin(playerObject, 0);
    }

    /// <summary>
    /// Reset the local player skin to the default skin.
    /// </summary>
    public void ResetLocalPlayerSkin() {
        UpdateLocalPlayerSkin(0);
    }

    /// <summary>
    /// Store the default player skin from the given hero controller.
    /// </summary>
    /// <param name="heroController">The HeroController instance.</param>
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
            Logger.Warn("Tried to store default player skin, but knight texture was null");
            return;
        }

        if (sprintTexture == null) {
            Logger.Warn("Tried to store default player skin, but sprint texture was null");
            return;
        }

        _defaultPlayerSkin = new PlayerSkin();
        _defaultPlayerSkin.SetKnightTexture(knightTexture);
        _defaultPlayerSkin.SetSprintTexture(sprintTexture);
    }
}
