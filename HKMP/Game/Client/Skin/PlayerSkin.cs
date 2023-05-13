using UnityEngine;

namespace Hkmp.Game.Client.Skin;

/// <summary>
/// Data class for player skin textures.
/// </summary>
internal class PlayerSkin {
    /// <summary>
    /// Whether this skin contains the knight texture.
    /// </summary>
    public bool HasKnightTexture { get; private set; }

    /// <summary>
    /// The knight texture for the skin, or null if it does not have it.
    /// </summary>
    public Texture KnightTexture { get; private set; }

    /// <summary>
    /// Whether this skin contains the sprint texture.
    /// </summary>
    public bool HasSprintTexture { get; private set; }

    /// <summary>
    /// The sprint texture for the skin, or null if it does not have it.
    /// </summary>
    public Texture SprintTexture { get; private set; }

    /// <summary>
    /// Set the knight texture for the skin.
    /// </summary>
    /// <param name="knightTexture">The knight texture.</param>
    public void SetKnightTexture(Texture knightTexture) {
        KnightTexture = knightTexture;
        HasKnightTexture = true;
    }

    /// <summary>
    /// Set the sprint texture for the skin.
    /// </summary>
    /// <param name="sprintTexture">The sprint texture.</param>
    public void SetSprintTexture(Texture sprintTexture) {
        SprintTexture = sprintTexture;
        HasSprintTexture = true;
    }
}
