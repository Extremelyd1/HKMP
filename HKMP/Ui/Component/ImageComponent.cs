using UnityEngine;
using UnityEngine.UI;

namespace Hkmp.Ui.Component {
    public class ImageComponent : Component {
        public ImageComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            Vector2 size,
            Texture2D texture
        ) : base(componentGroup, position, size) {
            var image = GameObject.AddComponent<Image>();
            image.sprite = CreateSpriteFromTexture(texture);
            image.type = Image.Type.Simple;
        }
    }
}