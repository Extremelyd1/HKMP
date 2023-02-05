using UnityEngine;
using UnityEngine.UI;

namespace Hkmp.Ui.Component;

/// <summary>
/// Simple component that displays an image.
/// </summary>
internal class ImageComponent : Component {
    public ImageComponent(
        ComponentGroup componentGroup,
        Vector2 position,
        Vector2 size,
        Sprite sprite
    ) : base(componentGroup, position, size) {
        var image = GameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
    }
}
