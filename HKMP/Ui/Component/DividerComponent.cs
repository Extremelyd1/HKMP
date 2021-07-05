using Hkmp.Ui.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace Hkmp.Ui.Component {
    public class DividerComponent : Component {
        public DividerComponent(ComponentGroup componentGroup, Vector2 position, Vector2 size) : base(componentGroup,
            position, size) {
            var image = GameObject.AddComponent<Image>();
            image.sprite = CreateSpriteFromTexture(TextureManager.Divider);
            image.type = Image.Type.Simple;
        }
    }
}