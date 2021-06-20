using System;
using Hkmp.Ui.Resources;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hkmp.Ui.Component {
    public class ButtonComponent : Component, IButtonComponent {
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
            // Create background image
            var image = GameObject.AddComponent<Image>();
            image.sprite = CreateSpriteFromTexture(texture);
            image.type = Image.Type.Simple;

            // Create the text component in the button
            var textObject = new GameObject();
            textObject.AddComponent<RectTransform>().sizeDelta = size;
            var textComponent = textObject.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = font;
            textComponent.fontSize = fontSize;
            textComponent.alignment = TextAnchor.MiddleCenter;
            textComponent.color = textColor;

            var textTransform = textComponent.transform;
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
            var buttonComponent = GameObject.AddComponent<Button>();
            buttonComponent.onClick.AddListener(() => { _onPress?.Invoke(); });
        }

        public void SetOnPress(Action action) {
            _onPress = action;
        }
    }
}