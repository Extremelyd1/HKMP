using System.Collections;
using Hkmp.Ui.Component;
using Hkmp.Util;
using UnityEngine;

namespace Hkmp.Ui.Chat;

/// <summary>
/// Class that manages a single message in chat.
/// </summary>
internal class ChatMessage {
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

    /// <summary>
    /// The current alpha of the message.
    /// </summary>
    private float _alpha;

    /// <summary>
    /// Whether this message is already completely faded out.
    /// </summary>
    private bool _isFadedOut;

    /// <summary>
    /// Whether the chat is currently open.
    /// </summary>
    private bool _chatOpen;

    /// <summary>
    /// Constructs the chat message in the given group at the given position and with the given text.
    /// </summary>
    /// <param name="componentGroup">The component group it should be in.</param>
    /// <param name="position">The position of the message.</param>
    /// <param name="text">The string text.</param>
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
            UiManager.ChatFontSize,
            alignment: TextAnchor.LowerLeft
        );
        _textComponent.SetActive(false);
        _alpha = 1f;
    }

    /// <summary>
    /// Displays the chat message and notes whether the chat is open or not.
    /// </summary>
    /// <param name="chatOpen">Whether the chat is open or not.</param>
    public void Display(bool chatOpen) {
        _chatOpen = chatOpen;

        _textComponent.SetActive(true);

        _fadeCoroutine = MonoBehaviourUtil.Instance.StartCoroutine(FadeRoutine());
    }

    /// <summary>
    /// Hides the chat message because it surpassed the maximum number of shown messages.
    /// </summary>
    public void Hide() {
        _isFadedOut = true;
        SetAlpha(1f);

        if (!_chatOpen) {
            _textComponent.SetActive(false);
        }

        if (_fadeCoroutine != null) {
            MonoBehaviourUtil.Instance.StopCoroutine(_fadeCoroutine);
        }
    }

    /// <summary>
    /// Indicates that the chat is opened or closed and will show/hide this chat message accordingly.
    /// </summary>
    /// <param name="chatOpen">Whether the chat is open or closed.</param>
    public void OnChatToggle(bool chatOpen) {
        _chatOpen = chatOpen;

        if (chatOpen) {
            if (_isFadedOut) {
                _textComponent.SetActive(true);
            } else {
                SetAlpha(1f);
            }
        } else {
            if (_isFadedOut) {
                _textComponent.SetActive(false);
            }
        }
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
    /// Destroy the text component of this chat message.
    /// </summary>
    public void Destroy() {
        _textComponent.Destroy();
    }

    /// <summary>
    /// Set the alpha of the text component.
    /// </summary>
    /// <param name="alpha">Float representing the new alpha value. Ranging from 0 to 1.</param>
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
            _alpha = 1f - normalizedTime;

            if (!_chatOpen) {
                SetAlpha(_alpha);
            }

            yield return null;
        }

        _fadeCoroutine = null;
        Hide();
    }
}
