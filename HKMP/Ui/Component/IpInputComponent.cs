using System.Collections.Generic;
using UnityEngine;

namespace Hkmp.Ui.Component;

/// <summary>
/// Input component specifically for the IP input.
/// </summary>
internal class IpInputComponent : HiddenInputComponent {
    /// <summary>
    /// List of characters that cannot be input in this field.
    /// </summary>
    private static readonly List<char> BlacklistedChars = new List<char> {
        ' ',
        '\n',
        '\t',
        '\v',
        '\f',
        '\b',
        '\r'
    };

    public IpInputComponent(
        ComponentGroup componentGroup,
        Vector2 position,
        string defaultValue,
        string placeholderText
    ) : base(
        componentGroup,
        position,
        defaultValue,
        placeholderText,
        UiManager.NormalFontSize
    ) {
        InputField.onValidateInput += (text, index, addedChar) => {
            if (BlacklistedChars.Contains(addedChar)) {
                return '\0';
            }

            return addedChar;
        };
    }
}
