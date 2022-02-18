using Hkmp.Ui.Resources;
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
            int fontSize,
            FontStyle fontStyle = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleCenter
        ) : base(componentGroup, position, size) {
            _text = text;
            
            // Create the unity text object and set the corresponding details
            _textObject = GameObject.AddComponent<Text>();
            _textObject.text = text;
            _textObject.font = FontManager.UIFontRegular;
            _textObject.fontSize = fontSize;
            _textObject.fontStyle = fontStyle;
            _textObject.alignment = alignment;
            _textObject.horizontalOverflow = HorizontalWrapMode.Wrap;
            _textObject.verticalOverflow = VerticalWrapMode.Overflow;

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