using System;
using Hkmp.Ui.Resources;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hkmp.Ui.Component {
    public class ButtonComponent : Component, IButtonComponent {
        private static readonly Color NotInteractableColor = Color.gray;

        private readonly Color _textColor;

        private readonly Button _button;
        private readonly Text _text;

        private Action _onPress;

        public ButtonComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            string text
        ) : this(
            componentGroup,
            position,
            new Vector2(200, 30),
            text,
            TextureManager.ButtonBackground,
            FontManager.UIFontRegular,
            16) {
        }

        public ButtonComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            Vector2 size,
            string text,
            Texture2D texture,
            Font font,
            int fontSize = 13
        ) : this(
            componentGroup,
            position,
            size,
            text,
            texture,
            font,
            Color.white,
            fontSize
        ) {
        }

        public ButtonComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            Vector2 size,
            string text,
            Texture2D texture,
            Font font,
            Color textColor,
            int fontSize = 13
        ) : base(componentGroup, position, size) {
            _textColor = textColor;
            
            // Create background image
            var image = GameObject.AddComponent<Image>();
            image.sprite = CreateSpriteFromTexture(texture);
            image.type = Image.Type.Simple;

            // Create the text component in the button
            var textObject = new GameObject();
            textObject.AddComponent<RectTransform>().sizeDelta = size;
            _text = textObject.AddComponent<Text>();
            _text.text = text;
            _text.font = font;
            _text.fontSize = fontSize;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.color = textColor;

            var textTransform = _text.transform;
            var textPosition = textTransform.position;

            textTransform.position = new Vector3(
                textPosition.x,
                textPosition.y - 2,
                textPosition.z
            );

            // Set the transform parent to the ButtonComponent gameObject
            textObject.transform.SetParent(GameObject.transform, false);
            Object.DontDestroyOnLoad(textObject);

            // Create the button component and add the click listener
            _button = GameObject.AddComponent<Button>();
            _button.onClick.AddListener(() => { _onPress?.Invoke(); });
        }

        public void SetOnPress(Action action) {
            _onPress = action;
        }

        public void SetInteractable(bool interactable) {
            _button.interactable = interactable;

            if (interactable) {
                _text.color = _textColor;
            } else {
                _text.color = NotInteractableColor;
            }
        }
    }
}