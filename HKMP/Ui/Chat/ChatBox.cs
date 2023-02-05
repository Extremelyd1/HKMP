using System;
using GlobalEnums;
using Hkmp.Api.Client;
using Hkmp.Game.Settings;
using Hkmp.Imports;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hkmp.Ui.Chat;

/// <summary>
/// The message box in the bottom left of the screen that shows information related to HKMP.
/// </summary>
internal class ChatBox : IChatBox {
    /// <summary>
    /// The maximum number of messages shown when chat is closed.
    /// </summary>
    private const int MaxMessages = 20;

    /// <summary>
    /// The maximum number of messages shown when chat is opened.
    /// </summary>
    private const int MaxShownMessages = 10;

    /// <summary>
    /// The maximum width of the chat input and chat box.
    /// </summary>
    private const float ChatWidth = 500f;

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
    private const float BoxInputMargin = 30f;

    /// <summary>
    /// The height of the chat input.
    /// </summary>
    private const float InputHeight = 30f;

    /// <summary>
    /// The margin of the chat input with the bottom of the screen.
    /// </summary>
    private const float InputMarginBottom = 20f;

    /// <summary>
    /// The margin of the chat with the left side of the screen.
    /// </summary>
    private const float MarginLeft = 25f;

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
    /// Text generator used to figure out the width of to-be created text.
    /// </summary>
    private readonly TextGenerator _textGenerator;

    /// <summary>
    /// Array containing the latest messages.
    /// </summary>
    private readonly ChatMessage[] _messages;

    /// <summary>
    /// The chat input component.
    /// </summary>
    private readonly ChatInputComponent _chatInput;

    /// <summary>
    /// Whether the chat is currently open.
    /// </summary>
    private bool _isOpen;

    /// <summary>
    /// Event that is called when the user submits a message in the chat input.
    /// </summary>
    public event Action<string> ChatInputEvent;

    /// <summary>
    /// Construct the chat box in the given group and with the given mod settings.
    /// </summary>
    /// <param name="chatBoxGroup">The component group it should be in.</param>
    /// <param name="modSettings">The current mod settings.</param>
    public ChatBox(ComponentGroup chatBoxGroup, ModSettings modSettings) {
        _chatBoxGroup = chatBoxGroup;

        _textGenerator = new TextGenerator();

        _messages = new ChatMessage[MaxMessages];

        _chatInput = new ChatInputComponent(
            chatBoxGroup,
            new Vector2(ChatWidth / 2f + MarginLeft, InputMarginBottom + InputHeight / 2f),
            new Vector2(ChatWidth, InputHeight),
            UiManager.ChatFontSize
        );
        _chatInput.SetActive(false);
        _chatInput.OnSubmit += chatInput => {
            if (chatInput.Length > 0) {
                ChatInputEvent?.Invoke(chatInput);
            }

            HideChatInput();
        };

        _isOpen = false;

        // Calculate these values beforehand so we can use them for each message
        MessageSize = new Vector2(ChatWidth + TextMargin, MessageHeight);
        _textGenSettings = new TextGenerationSettings {
            font = FontManager.UIFontRegular,
            color = Color.white,
            fontSize = UiManager.ChatFontSize,
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
            pivot = new Vector2(0.5f, 0.5f),
            generateOutOfBounds = false
        };

        // Register the update event so we can check key binds
        MonoBehaviourUtil.Instance.OnUpdateEvent += () => CheckKeyBinds(modSettings);
    }

    /// <summary>
    /// Check whether key-binds for the chat box are pressed.
    /// </summary>
    /// <param name="modSettings">The mod settings instance.</param>
    private void CheckKeyBinds(ModSettings modSettings) {
        if (!_chatBoxGroup.IsActive()) {
            return;
        }

        if (_isOpen) {
            if (InputHandler.Instance.inputActions.pause.WasPressed) {
                HideChatInput();
            }
        } else if (Input.GetKeyDown(modSettings.OpenChatKey)) {
            var gameManager = GameManager.instance;
            var uiManager = UIManager.instance;
            var heroController = HeroController.instance;
            if (gameManager == null
                || uiManager == null
                || gameManager.gameState != GameState.PLAYING
                || uiManager.uiState != UIState.PLAYING
                // If the hero is charging their nail and chat opens, it will cause a flashing effect
                || (heroController != null && heroController.cState.nailCharging)
                // If we are in the inventory, opening the chat has side-effects, such as floating
                || IsInventoryOpen()
                // If we are in a godhome menu, we will soft-lock opening the chat
                || IsGodHomeMenuOpen()
               ) {
                return;
            }

            _isOpen = true;

            for (var i = 0; i < MaxMessages; i++) {
                _messages[i]?.OnChatToggle(true);
            }

            _chatInput.SetActive(true);
            _chatInput.Focus();

            InputHandler.Instance.StopMouseInput();
            InputHandler.Instance.PreventPause();
            SetEnabledHeroActions(false);
            DebugMod.SetLockKeyBinds(true);
        }
    }

    /// <summary>
    /// Hide the chat input component.
    /// </summary>
    private void HideChatInput() {
        _isOpen = false;

        for (var i = 0; i < MaxMessages; i++) {
            _messages[i]?.OnChatToggle(false);
        }

        _chatInput.SetActive(false);

        InputHandler.Instance.EnableMouseInput();
        // In case we were using keys in chat that also correspond to input actions
        InputHandler.Instance.inputActions.pause.ClearInputState();
        InputHandler.Instance.AllowPause();
        SetEnabledHeroActions(true);
        DebugMod.SetLockKeyBinds(false);
    }

    /// <inheritdoc />
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
            if (textWidth > ChatWidth) {
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

    /// <summary>
    /// Add a message to the chat box that is trimmed to the length that fits on a single line.
    /// </summary>
    /// <param name="messageText">The trimmed message string.</param>
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
                message.Hide();
            }

            // Move the message in the message array
            _messages[i + 1] = message;
        }

        // Create new message object at the lowest position in the chat box
        var newMessage = new ChatMessage(
            _chatBoxGroup,
            new Vector2(MessageSize.x / 2f + MarginLeft, InputMarginBottom + InputHeight + BoxInputMargin),
            messageText
        );
        newMessage.Display(_isOpen);

        // Assign it at the start of the array
        _messages[0] = newMessage;
    }

    /// <summary>
    /// Set whether the hero actions for input are enabled.
    /// </summary>
    /// <param name="enabled">The new boolean value for enabled.</param>
    private static void SetEnabledHeroActions(bool enabled) {
        var inputHandler = InputHandler.Instance;
        if (inputHandler == null) {
            return;
        }

        var heroActions = inputHandler.inputActions;
        if (heroActions == null) {
            return;
        }

        // Disable all input actions for the hero, except for the pause actions
        // which we use to listen for closing the chat again
        heroActions.left.Enabled = enabled;
        heroActions.right.Enabled = enabled;
        heroActions.up.Enabled = enabled;
        heroActions.down.Enabled = enabled;
        heroActions.menuSubmit.Enabled = enabled;
        heroActions.menuCancel.Enabled = enabled;
        heroActions.rs_up.Enabled = enabled;
        heroActions.rs_down.Enabled = enabled;
        heroActions.rs_left.Enabled = enabled;
        heroActions.rs_right.Enabled = enabled;
        heroActions.jump.Enabled = enabled;
        heroActions.evade.Enabled = enabled;
        heroActions.dash.Enabled = enabled;
        heroActions.superDash.Enabled = enabled;
        heroActions.dreamNail.Enabled = enabled;
        heroActions.attack.Enabled = enabled;
        heroActions.cast.Enabled = enabled;
        heroActions.focus.Enabled = enabled;
        heroActions.quickMap.Enabled = enabled;
        heroActions.quickCast.Enabled = enabled;
        heroActions.textSpeedup.Enabled = enabled;
        heroActions.skipCutscene.Enabled = enabled;
        heroActions.openInventory.Enabled = enabled;
        heroActions.paneRight.Enabled = enabled;
        heroActions.paneLeft.Enabled = enabled;
    }

    /// <summary>
    /// Checks whether the inventory is open.
    /// </summary>
    /// <returns>true if the inventory is open; otherwise false.</returns>
    private static bool IsInventoryOpen() {
        var gameManager = GameManager.instance;
        if (gameManager == null) {
            return false;
        }

        var inventoryFsm = gameManager.inventoryFSM;
        var stateName = inventoryFsm.ActiveStateName;
        return stateName != "Closed" && stateName != "Can Open Inventory?";
    }

    /// <summary>
    /// Checks whether either a pantheon challenge UI or a boss challenge UI from Godhome is open. 
    /// </summary>
    /// <returns>true if either challenge UI from pantheons or bosses is open; otherwise false.</returns>
    private static bool IsGodHomeMenuOpen() {
        var bossChallengeUi = Object.FindObjectsOfType<BossChallengeUI>();
        var bossDoorChallengeUi = Object.FindObjectsOfType<BossDoorChallengeUI>();

        return bossChallengeUi.Length != 0 || bossDoorChallengeUi.Length != 0;
    }
}
