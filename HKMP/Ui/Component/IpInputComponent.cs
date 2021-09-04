using System.Collections.Generic;
using UnityEngine;

namespace Hkmp.Ui.Component {
    public class IpInputComponent : HiddenInputComponent {
        private static readonly List<int> BlacklistedChars = new List<int> {
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
            placeholderText
        ) {
            InputField.onValidateInput += (text, index, addedChar) => {
                if (BlacklistedChars.Contains(addedChar)) {
                    return '\0';
                }

                return addedChar;
            };
        }
    }
}