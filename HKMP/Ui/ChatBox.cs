using System.Collections;
using Hkmp.Api.Client;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Ui {
    /// <summary>
    /// The message box in the bottom right of the screen that shows information related to HKMP.
    /// </summary>
    public class ChatBox : IChatBox {
        /// <summary>
        /// The maximum number of message at one time in the chat box.
        /// </summary>
        private const int MaxMessages = 10;

        /// <summary>
        /// The time a message stays after appearing in seconds.
        /// </summary>
        private const float MessageStayTime = 7.5f;

        /// <summary>
        /// The time a message takes to fade out in seconds.
        /// </summary>
        private const float MessageFadeTime = 1f;

        /// <summary>
        /// The font size of the messages.
        /// </summary>
        private const int FontSize = 15;

        /// <summary>
        /// The maximum width of messages in the chat box.
        /// </summary>
        private const float BoxWidth = 500f;

        /// <summary>
        /// Margin for text to make sure it fits within the box and doesn't get cut off by Unity.
        /// </summary>
        private const float TextMargin = 10f;

        /// <summary>
        /// The height of messages.
        /// </summary>
        private const float MessageHeight = 25f;

        /// <summary>
        /// The margin of the chat box with the bottom of the screen.
        /// </summary>
        private const float BoxMarginBottom = 75f;

        /// <summary>
        /// The margin of the chat box with the right side of the screen.
        /// </summary>
        private const float BoxMarginRight = 50f;

        /// <summary>
        /// The size of new message added to the chat box.
        /// </summary>
        private static Vector2 _messageSize;

        /// <summary>
        /// Text generation settings used to figure out the width of to-be created text.
        /// </summary>
        private static TextGenerationSettings _textGenSettings;

        /// <summary>
        /// The component group of this chat box and all messages in it.
        /// </summary>
        private readonly ComponentGroup _chatBoxGroup;

        /// <summary>
        /// Array containing the latest messages.
        /// </summary>
        private readonly TextComponent[] _messages;

        /// <summary>
        /// Text generator used to figure out the width of to-be created text.
        /// </summary>
        private readonly TextGenerator _textGenerator;

        public ChatBox(ComponentGroup chatBoxGroup) {
            _chatBoxGroup = chatBoxGroup;

            _messages = new TextComponent[MaxMessages];

            _textGenerator = new TextGenerator();

            // Calculate these values beforehand so we can use them for each message
            _messageSize = new Vector2(BoxWidth + TextMargin, MessageHeight);
            _textGenSettings = new TextGenerationSettings {
                font = FontManager.UIFontRegular,
                color = Color.white,
                fontSize = FontSize,
                lineSpacing = 1,
                richText = true,
                scaleFactor = 1,
                fontStyle = FontStyle.Normal,
                textAnchor = TextAnchor.LowerLeft,
                alignByGeometry = false,
                resizeTextForBestFit = false,
                resizeTextMinSize = 10,
                resizeTextMaxSize = 40,
                updateBounds = false,
                verticalOverflow = VerticalWrapMode.Overflow,
                horizontalOverflow = HorizontalWrapMode.Wrap,
                generationExtents = _messageSize,
                pivot = new Vector2(0.5f, 1.0f),
                generateOutOfBounds = false
            };
        }

        public void AddMessage(string messageText) {
            var textChars = messageText.ToCharArray();
            // Keep track of the index of the last space character so we know where to split the message
            var lastSpaceIndex = 0;

            for (var i = 0; i < textChars.Length; i++) {
                // Check whether the current character is a space
                if (textChars[i] == ' ') {
                    lastSpaceIndex = i;
                }

                // Get the substring of text up until this index and calculate the width
                var text = messageText.Substring(0, i + 1);
                var textWidth = _textGenerator.GetPreferredWidth(text, _textGenSettings);

                // Check whether we have exceeded the width of the chat box with this substring
                if (textWidth > BoxWidth) {
                    if (lastSpaceIndex != 0) {
                        // Add the part until the last space as a message
                        AddTrimmedMessage(messageText.Substring(0, lastSpaceIndex));

                        // Recursively parse the rest of the text without the space
                        AddMessage(messageText.Substring(lastSpaceIndex + 1));
                    } else {
                        // There wasn't a last space to split the message on, so we split on the previous character
                        AddTrimmedMessage(messageText.Substring(0, i));

                        // Recursively parse the rest of the text
                        AddMessage(messageText.Substring(i));
                    }

                    return;
                }
            }

            // The message did not get split up, so we add it in its entirety
            AddTrimmedMessage(messageText);
        }

        private void AddTrimmedMessage(string messageText) {
            // Conditionally destroy the oldest message
            _messages[MaxMessages - 1]?.Destroy();

            for (var i = MaxMessages - 2; i >= 0; i--) {
                var messageObject = _messages[i];

                if (messageObject == null) {
                    continue;
                }

                // Move the message object to its new position
                messageObject.SetPosition(messageObject.GetPosition() + new Vector2(0, MessageHeight));

                // Move the message in the message array
                _messages[i + 1] = messageObject;
            }

            var newMessageObject = CreateMessageObject(messageText,
                new Vector2(1920f - BoxWidth / 2f - BoxMarginRight, BoxMarginBottom));
            _messages[0] = newMessageObject;

            // Start a coroutine that will fade away the message after a delay
            MonoBehaviourUtil.Instance.StartCoroutine(WaitFadeMessage(newMessageObject));
        }

        /// <summary>
        /// Create a new message object from the given message content and position.
        /// </summary>
        /// <param name="message">The text that the message should have.</param>
        /// <param name="position">The position of the message object.</param>
        /// <returns></returns>
        private TextComponent CreateMessageObject(string message, Vector2 position) {
            return new TextComponent(
                _chatBoxGroup,
                position,
                _messageSize,
                message,
                FontManager.UIFontRegular,
                FontSize,
                alignment: TextAnchor.LowerLeft
            );
        }

        /// <summary>
        /// Wait for a certain amount of time and then fade out the message by reducing the alpha gradually.
        /// </summary>
        /// <param name="textComponent">The text component of the message.</param>
        private static IEnumerator WaitFadeMessage(TextComponent textComponent) {
            yield return new WaitForSeconds(MessageStayTime);

            for (var t = 0f; t < MessageFadeTime; t += Time.deltaTime) {
                var normalizedTime = t / MessageFadeTime;
                var alpha = 1f - normalizedTime;

                var color = textComponent.GetColor();
                textComponent.SetColor(new Color(color.r, color.g, color.b, alpha));

                yield return null;
            }
        }
    }
}