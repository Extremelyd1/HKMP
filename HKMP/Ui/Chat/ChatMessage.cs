using System.Collections;
using Hkmp.Ui;
using Hkmp.Ui.Chat;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;

namespace HKMP.Ui.Chat {
    public class ChatMessage {
        /// <summary>
        /// The time a message stays after appearing in seconds.
        /// </summary>
        private const float MessageStayTime = 7.5f;

        /// <summary>
        /// The time a message takes to fade out in seconds.
        /// </summary>
        private const float MessageFadeTime = 1f;

        /// <summary>
        /// The text component belonging to this chat message.
        /// </summary>
        private readonly TextComponent _textComponent;
        /// <summary>
        /// The current coroutine responsible for fading out the message after a delay.
        /// </summary>
        private Coroutine _fadeCoroutine;

        public ChatMessage(
            ComponentGroup componentGroup,
            Vector2 position,
            string text
        ) {
            _textComponent = new TextComponent(
                componentGroup,
                position,
                ChatBox.MessageSize,
                text,
                FontManager.UIFontRegular,
                ChatBox.FontSize,
                alignment: TextAnchor.LowerLeft
            );
        }

        /// <summary>
        /// Set the chat message to be active or inactive.
        /// </summary>
        /// <param name="active">The new 'active' value.</param>
        public void SetActive(bool active) {
            _textComponent.SetActive(active);
        }

        /// <summary>
        /// Move this chat message by the given position. This will add the given position to the current position
        /// and set the new position of the chat message as the result.
        /// </summary>
        /// <param name="position">Vector2 of the position it should be moved by.</param>
        public void Move(Vector2 position) {
            _textComponent.SetPosition(_textComponent.GetPosition() + position);
        }

        /// <summary>
        /// Start fading out this message after an initial delay.
        /// </summary>
        public void StartFade() {
            if (_fadeCoroutine != null) {
                MonoBehaviourUtil.Instance.StopCoroutine(_fadeCoroutine);
                SetAlpha(1f);
            }
            
            _fadeCoroutine = MonoBehaviourUtil.Instance.StartCoroutine(FadeRoutine());
        }

        /// <summary>
        /// Destroy the text component of this chat message.
        /// </summary>
        public void Destroy() {
            _textComponent.Destroy();
        }

        /// <summary>
        /// Set the alpha of the color of the text component.
        /// </summary>
        /// <param name="alpha">Float value representing the new alpha value. Possible values from 0 to 1.</param>
        private void SetAlpha(float alpha) {
            var color = _textComponent.GetColor();
            _textComponent.SetColor(new Color(color.r, color.g, color.b, alpha));
        }

        /// <summary>
        /// Wait for a certain amount of time and then fade out the message by reducing the alpha gradually.
        /// </summary>
        private IEnumerator FadeRoutine() {
            yield return new WaitForSeconds(MessageStayTime);
            
            for (var t = 0f; t < MessageFadeTime; t += Time.deltaTime) {
                var normalizedTime = t / MessageFadeTime;
                var alpha = 1f - normalizedTime;

                SetAlpha(alpha);

                yield return null;
            }
        }
    }
}