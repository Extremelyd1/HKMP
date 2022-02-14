using Hkmp.Api.Client;
using HKMP.Ui.Chat;
using Hkmp.Ui.Resources;
using UnityEngine;

namespace Hkmp.Ui.Chat {
    /// <summary>
    /// The message box in the bottom right of the screen that shows information related to HKMP.
    /// </summary>
    public class ChatBox : IChatBox {
        /// <summary>
        /// The maximum number of messages shown when chat is closed.
        /// </summary>
        private const int MaxMessages = 20;

        /// <summary>
        /// The maximum number of messages shown when chat is opened.
        /// </summary>
        private const int MaxShownMessages = 10;

        /// <summary>
        /// The font size of the messages.
        /// </summary>
        public const int FontSize = 15;

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
        /// The margin of the chat box with the left side of the screen.
        /// </summary>
        private const float BoxMarginLeft = 25f;

        /// <summary>
        /// The size of new message added to the chat box.
        /// </summary>
        public static Vector2 MessageSize;

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
        private readonly ChatMessage[] _messages;

        /// <summary>
        /// Text generator used to figure out the width of to-be created text.
        /// </summary>
        private readonly TextGenerator _textGenerator;

        public ChatBox(ComponentGroup chatBoxGroup) {
            _chatBoxGroup = chatBoxGroup;

            _messages = new ChatMessage[MaxMessages];

            _textGenerator = new TextGenerator();

            // Calculate these values beforehand so we can use them for each message
            MessageSize = new Vector2(BoxWidth + TextMargin, MessageHeight);
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
                generationExtents = MessageSize,
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
                var message = _messages[i];

                if (message == null) {
                    continue;
                }

                // Move the message object to its new position
                message.Move(new Vector2(0, MessageHeight));

                if (i >= MaxShownMessages - 1) {
                    // If this message should no longer be shown without chat open we deactivate it
                    message.SetActive(false);
                }
                
                // Move the message in the message array
                _messages[i + 1] = message;
            }

            // Create new message object at the lowest position in the chat box
            var newMessage = new ChatMessage(
                _chatBoxGroup,
                new Vector2(BoxWidth / 2f + BoxMarginLeft, BoxMarginBottom),
                messageText
            );
            // Immediately start the coroutine for fading out the message
            newMessage.StartFade();
            
            // Assign it at the start of the array
            _messages[0] = newMessage;
        }
    }
}