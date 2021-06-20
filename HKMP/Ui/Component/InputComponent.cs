using Hkmp.Ui.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace Hkmp.Ui.Component {
    public class InputComponent : Component, IInputComponent {
        private readonly InputField _inputField;

        public InputComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            string defaultValue,
            string placeholderText,
            int fontSize = 18,
            InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None
        ) : this(
            componentGroup,
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

        public InputComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            Vector2 size,
            string defaultValue,
            string placeholderText,
            Texture2D texture,
            Font font,
            int fontSize = 13,
            InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None
        ) : base(componentGroup, position, size) {
            // Create background image
            var image = GameObject.AddComponent<Image>();
            image.sprite = CreateSpriteFromTexture(texture);
            image.type = Image.Type.Simple;

            var placeholder = new GameObject();
            placeholder.AddComponent<RectTransform>().sizeDelta = size;
            var placeholderTextComponent = placeholder.AddComponent<Text>();
            placeholderTextComponent.text = placeholderText;
            placeholderTextComponent.font = font;
            placeholderTextComponent.fontSize = fontSize;
            placeholderTextComponent.alignment = TextAnchor.MiddleCenter;
            // Make the color black with opacity so it is clearly different from inputted text
            placeholderTextComponent.color = new Color(0, 0, 0, 0.5f);

            // Set the transform parent to the InputComponent gameObject
            placeholder.transform.SetParent(GameObject.transform, false);
            Object.DontDestroyOnLoad(placeholder);

            var textObject = new GameObject();
            textObject.AddComponent<RectTransform>().sizeDelta = size;
            var textComponent = textObject.AddComponent<Text>();
            textComponent.text = defaultValue;
            textComponent.font = font;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = Color.black;

            // Set the transform parent to the InputComponent gameObject
            textObject.transform.SetParent(GameObject.transform, false);
            Object.DontDestroyOnLoad(textObject);

            // Create the actual inputField component
            _inputField = GameObject.AddComponent<InputField>();
            _inputField.targetGraphic = image;
            _inputField.placeholder = placeholderTextComponent;
            _inputField.textComponent = textComponent;
            _inputField.text = defaultValue;
            _inputField.characterValidation = characterValidation;
        }

        public string GetInput() {
            return _inputField.text;
        }
    }
}