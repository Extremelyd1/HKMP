using Hkmp.Ui.Resources;
using UnityEngine;

namespace Hkmp.Ui.Component {
    public class ChatInputComponent : InputComponent {
        public ChatInputComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            Vector2 size,
            int fontSize
        ) : base(
            componentGroup,
            position,
            size,
            "",
            "",
            TextureManager.InputFieldBg,
            FontManager.UIFontRegular,
            fontSize
        ) {
            Text.alignment = TextAnchor.MiddleLeft;
            Text.color = Color.white;
        }

        public void Focus() {
            InputField.ActivateInputField();
        }
    }
}