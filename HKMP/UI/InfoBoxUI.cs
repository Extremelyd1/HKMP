using System.Collections;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using HKMP.Util;
using UnityEngine;

namespace HKMP.UI {
    public class InfoBoxUI {
        // The maximum number of message at one time in the info box
        private const int MaxMessages = 10;
        // The time a message stays after appearing
        private const float MessageStayTime = 7.5f;
        // The time a message takes to fade out
        private const float MessageFadeTime = 1f;

        // The font size of the messages
        private const int FontSize = 13;
        // The maximum width of messages in the info box
        private const int InfoBoxWidth = 300;
        // The height of messages
        private const float MessageHeight = 25f;
        // The margin of the info box with the borders of the screen
        private const int InfoBoxMargin = 50;

        private readonly UIGroup _infoBoxGroup;
        private readonly TextComponent[] _messages;

        public InfoBoxUI(UIGroup infoBoxGroup) {
            _infoBoxGroup = infoBoxGroup;

            _messages = new TextComponent[MaxMessages];
        }

        public void AddMessage(string messageText) {
            var textChars = messageText.ToCharArray();
            var font = FontManager.UIFontRegular;

            var textWidth = 0;
            var lastSpaceIndex = 0;
            for (var i = 0; i < textChars.Length; i++) {
                var textChar = textChars[i];

                if (textChar == ' ') {
                    lastSpaceIndex = i;
                }
                
                font.GetCharacterInfo(textChar, out var characterInfo, FontSize);
                textWidth += characterInfo.advance;

                if (textWidth > InfoBoxWidth && lastSpaceIndex != 0) {
                    // Add the first part of the text as a message
                    AddTrimmedMessage(messageText.Substring(0, lastSpaceIndex));
                    
                    // Recursively parse the rest of the text without the space
                    AddMessage(messageText.Substring(lastSpaceIndex + 1));
                    return;
                }
            }
            
            // The message did not get split up, so we add it in its entirety
            AddTrimmedMessage(messageText);
        }

        public void AddTrimmedMessage(string messageText) {
            _messages[MaxMessages - 1]?.Destroy();

            for (var i = MaxMessages - 2; i >= 0; i--) {
                var messageObject = _messages[i];
                
                if (messageObject == null) {
                    continue;
                }
               
                messageObject.SetPosition(messageObject.GetPosition() + new Vector2(0, MessageHeight));

                _messages[i + 1] = messageObject;
            }

            var newMessageObject = CreateMessageObject(messageText, new Vector2(1920f - InfoBoxWidth / 2 - InfoBoxMargin, InfoBoxMargin));
            _messages[0] = newMessageObject;
            
            MonoBehaviourUtil.Instance.StartCoroutine(WaitMessageStay(newMessageObject));
        }

        private TextComponent CreateMessageObject(string message, Vector2 position) {
            return new TextComponent(
                _infoBoxGroup,
                position,
                new Vector2(InfoBoxWidth, MessageHeight),
                message,
                FontManager.UIFontRegular,
                FontSize,
                alignment: TextAnchor.LowerLeft
            );
        }

        private IEnumerator WaitMessageStay(TextComponent textComponent) {
            yield return new WaitForSeconds(MessageStayTime);

            MonoBehaviourUtil.Instance.StartCoroutine(FadeMessageOut(textComponent, MessageFadeTime));
        }
        
        private IEnumerator FadeMessageOut(TextComponent textComponent, float duration) {
            for (var t = 0f; t < duration; t += Time.deltaTime) {
                var normalizedTime = t / duration;
                var alpha = 1f - normalizedTime;

                var color = textComponent.GetColor();
                textComponent.SetColor(new Color(color.r, color.g, color.b, alpha));
                
                yield return null;
            }
        }
    }
}