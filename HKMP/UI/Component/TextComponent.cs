using UnityEngine;
using UnityEngine.UI;

namespace HKMP.UI.Component {
    public class TextComponent : Component, ITextComponent {
        private readonly Text _textObject;
        
        public TextComponent(GameObject parent, Vector2 position, Vector2 size, string text, Font font, int fontSize = 13,
            FontStyle fontStyle = FontStyle.Normal, TextAnchor alignment = TextAnchor.MiddleCenter) : base(parent, position, size) {
            // Create the unity text object and set the corresponding details
            _textObject = GameObject.AddComponent<Text>();
            _textObject.text = text;
            _textObject.font = font;
            _textObject.fontSize = fontSize;
            _textObject.fontStyle = fontStyle;
            _textObject.alignment = alignment;
            _textObject.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Add a black outline to the text
            var outline = GameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
        }

        public void SetText(string text) {
            _textObject.text = text;
        }

        public void SetColor(Color color) {
            _textObject.color = color;
        }

        public float GetHeight() {
            return _textObject.flexibleHeight;
        }
    }
}