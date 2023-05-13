using System;
using System.Collections.Generic;
using Hkmp.Networking.Packet.Data;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;
using UnityEngine.UI;

namespace Hkmp.Ui.Component;

/// <summary>
/// An input component specifically for the chat.
/// </summary>
internal class ChatInputComponent : InputComponent {
    /// <summary>
    /// Action that is executed when the user submits the input field.
    /// </summary>
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

        MonoBehaviourUtil.Instance.OnUpdateEvent += () => {
            if (Input.GetKeyDown(KeyCode.Return)) {
                OnSubmit?.Invoke(InputField.text);

                InputField.text = "";
            }
        };
    }

    /// <summary>
    /// Focus the input field.
    /// </summary>
    public void Focus() {
        InputField.ActivateInputField();
    }
}
