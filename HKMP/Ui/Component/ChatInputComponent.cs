using System;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Ui.Component {
    public class ChatInputComponent : InputComponent {
        public event Action<string> OnSubmit;

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

            MonoBehaviourUtil.Instance.OnUpdateEvent += () => {
                if (Input.GetKeyDown(KeyCode.Return)) {
                    if (InputField.text.Length > 0) {
                        OnSubmit?.Invoke(InputField.text);

                        InputField.text = "";
                    }
                }
            };
        }

        public void Focus() {
            InputField.ActivateInputField();
        }
    }
}