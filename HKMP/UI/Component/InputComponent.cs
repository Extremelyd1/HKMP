using HKMP.UI.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class InputComponent : Component, IInputComponent {
        private readonly Text _textObject;

        public InputComponent(GameObject parent, Vector2 position, string defaultValue, string placeholderText, int fontSize = 18, InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None) :
            this(parent, position, new Vector2(200, 30), defaultValue, placeholderText,
                TextureManager.GetTexture("input_field_background"), FontManager.GetFont(UIManager.TrajanProName), fontSize, characterValidation) {
        }

        public InputComponent(GameObject parent, Vector2 position, Vector2 size, string defaultValue,
            string placeholderText, Texture2D texture, Font font,
            int fontSize = 13, InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None)
            : base(parent, position, size) {
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
            _textObject = textObject.AddComponent<Text>();
            _textObject.text = defaultValue;
            _textObject.font = font;
            _textObject.fontSize = fontSize;
            _textObject.alignment = TextAnchor.MiddleCenter;
            _textObject.color = Color.black;

            // Set the transform parent to the InputComponent gameObject
            textObject.transform.SetParent(GameObject.transform, false);
            Object.DontDestroyOnLoad(textObject);

            // Create the actual inputField component
            var inputField = GameObject.AddComponent<InputField>();
            inputField.targetGraphic = image;
            inputField.placeholder = placeholderTextComponent;
            inputField.textComponent = _textObject;
            inputField.text = defaultValue;
            inputField.characterValidation = characterValidation;
        }

        public string GetInput() {
            return _textObject.text;
        }

        public void SetPlaceholder(string text) {
            throw new System.NotImplementedException();
        }
    }
}