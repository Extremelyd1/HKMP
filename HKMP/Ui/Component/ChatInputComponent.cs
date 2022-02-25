using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Ui.Component {
    public class ChatInputComponent : InputComponent {
        private static readonly List<char> WhitelistedChars;

        static ChatInputComponent() {
            WhitelistedChars = new List<char>();
            foreach (var character in ChatMessage.AllowedCharacterString.ToCharArray()) {
                WhitelistedChars.Add(character);
            }
        }

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

            InputField.characterLimit = ChatMessage.MaxMessageLength;
            InputField.onValidateInput += (text, index, addedChar) => {
                if (!WhitelistedChars.Contains(addedChar)) {
                    return '\0';
                }

                return addedChar;
            };

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