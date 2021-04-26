using HKMP.UI.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class DividerComponent : Component {
        
        public DividerComponent(GameObject parent, Vector2 position, Vector2 size) : base(parent, position, size) {
            var image = GameObject.AddComponent<Image>();
            image.sprite = CreateSpriteFromTexture(TextureManager.Divider);
            image.type = Image.Type.Simple;
        }
    }
}