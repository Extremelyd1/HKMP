using HKMP.UI.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class HiddenInputComponent : InputComponent {
        public HiddenInputComponent(
            UIGroup uiGroup, 
            Vector2 position, 
            string defaultValue, 
            string placeholderText, 
            int fontSize = 18, 
            InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None
        ) : this(
            uiGroup, 
            position,
            new Vector2(200, 30),
            defaultValue, 
            placeholderText,
            TextureManager.InputFieldBackground,
            FontManager.UIFontRegular,
            fontSize, 
            characterValidation
        ) {
        }

        public HiddenInputComponent(
            UIGroup uiGroup, 
            Vector2 position, 
            Vector2 size, 
            string defaultValue, 
            string placeholderText, 
            Texture2D texture, 
            Font font, 
            int fontSize = 13, 
            InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None
        ) : base(
            uiGroup, 
            position, 
            size, 
            defaultValue, 
            placeholderText, 
            texture, 
            font, 
            fontSize, 
            characterValidation
        ) {
            var buttonComponent = new ButtonComponent(
                uiGroup,
                position,
                size,
                "Click to show",
                texture,
                font,
                new Color(0, 0, 0, 0.5f),
                fontSize
            );
            
            // Disable the original input component object
            GameObject.SetActive(false);

            // Hide our block object and show the input component on click
            buttonComponent.SetOnPress(() => {
                buttonComponent.SetActive(false);
                GameObject.SetActive(true);
            });
            
            // Add a handler for when we leave the component with our cursor,
            // which is when we enable the block object again and hide the input component
            GameObject.AddComponent<HiddenButtonLeaveHandler>().Action = () => {
                buttonComponent.SetActive(true);
                GameObject.SetActive(false);
            };
        }
    }
}