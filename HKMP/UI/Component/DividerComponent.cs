using HKMP.UI.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class DividerComponent : Component {
        
        public DividerComponent(UIGroup uiGroup, Vector2 position, Vector2 size) : base(uiGroup, position, size) {
            var image = GameObject.AddComponent<Image>();
            image.sprite = CreateSpriteFromTexture(TextureManager.Divider);
            image.type = Image.Type.Simple;
        }
    }
}