using UnityEngine;
using UnityEngine.UI;

namespace Hkmp.Ui.Component {
    public class TextComponent : Component, ITextComponent {
        private readonly Text _textObject;
        private readonly string _text;

        public TextComponent(
            ComponentGroup componentGroup,
            Vector2 position,
            Vector2 size,
            string text,
            Font font,
            int fontSize = 13,
            FontStyle fontStyle = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleCenter
        ) : base(componentGroup, position, size) {
            _text = text;
            
            // Create the unity text object and set the corresponding details
            _textObject = GameObject.AddComponent<Text>();
            _textObject.text = text;
            _textObject.font = font;
            _textObject.fontSize = fontSize;
            _textObject.fontStyle = fontStyle;
            _textObject.alignment = alignment;
            _textObject.horizontalOverflow = HorizontalWrapMode.Wrap;
            _textObject.verticalOverflow = VerticalWrapMode.Overflow;
            
            _textObject.rectTransform.pivot = new Vector2(0.5f, 1f);

            // Add a content size fitter to wrap text that overflows
            var sizeFitter = GameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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

        public Color GetColor() {
            return _textObject.color;
        }

        public float GetPreferredWidth() {
            var textGen = new TextGenerator();
            var genSettings = _textObject.GetGenerationSettings(_textObject.rectTransform.rect.size);

            return textGen.GetPreferredWidth(_text, genSettings);
        }
    }
}