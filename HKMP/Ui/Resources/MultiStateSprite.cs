using UnityEngine;

namespace Hkmp.Ui.Resources;

/// <summary>
/// Struct for multi-state UI sprites.
/// </summary>
public struct MultiStateSprite {
    /// <summary>
    /// The neutral sprite (non-hover, non-click, enabled).
    /// </summary>
    public Sprite Neutral { get; set; }

    /// <summary>
    /// The hover sprite (hover, non-click, enabled).
    /// </summary>
    public Sprite Hover { get; set; }

    /// <summary>
    /// The active sprite (clicked, enabled).
    /// </summary>
    public Sprite Active { get; set; }

    /// <summary>
    /// The disabled sprite.
    /// </summary>
    public Sprite Disabled { get; set; }
}
