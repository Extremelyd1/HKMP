using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class ImageComponent : Component {
        public ImageComponent(
            UIGroup uiGroup, 
            Vector2 position, 
            Vector2 size,
            Texture2D texture
        ) : base(uiGroup, position, size) {
            var image = GameObject.AddComponent<Image>();
            image.sprite = CreateSpriteFromTexture(texture);
            image.type = Image.Type.Simple;
        }
    }
}