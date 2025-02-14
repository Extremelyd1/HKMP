using System;
using System.Collections;
using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Ui;

/// <summary>
/// Class for creating and managing the connect interface.
/// </summary>
internal class ConnectInterface {
    /// <summary>
    /// The indent of some text elements.
    /// </summary>
    private const float TextIndentWidth = 5f;

    /// <summary>
    /// The text of the connection button if not connected.
    /// </summary>
    private const string ConnectText = "Connect";

    /// <summary>
    /// The text of the connection button while connecting.
    /// </summary>
    private const string ConnectingText = "Connecting...";

    /// <summary>
    /// The text of the host button while not hosting.
    /// </summary>
    private const string StartHostingText = "Start Hosting";

    /// <summary>
    /// The time in seconds to hide the feedback text after it appeared.
    /// </summary>
    private const float FeedbackTextHideTime = 10f;

    /// <summary>
    /// The mod settings.
    /// </summary>
    private readonly ModSettings _modSettings;

    /// <summary>
    /// The component group of the connect UI.
    /// </summary>
    private readonly ComponentGroup _connectGroup;

    // /// <summary>
    // /// The component group of the client settings UI.
    // /// </summary>
    // private readonly ComponentGroup _settingsGroup;

    /// <summary>
    /// The username input component.
    /// </summary>
    private IInputComponent _usernameInput;

    /// <summary>
    /// The address input component.
    /// </summary>
    private IInputComponent _addressInput;

    /// <summary>
    /// The port input component.
    /// </summary>
    private IInputComponent _portInput;

    /// <summary>
    /// The connection (connect or disconnect) button component.
    /// </summary>
    private IButtonComponent _connectionButton;

    /// <summary>
    /// The server host button component.
    /// </summary>
    private IButtonComponent _serverButton;

    /// <summary>
    /// The feedback text component.
    /// </summary>
    private ITextComponent _feedbackText;

    /// <summary>
    /// The coroutine that hides the feedback text after a delay.
    /// </summary>
    private Coroutine _feedbackHideCoroutine;

    /// <summary>
    /// Event that is executed when the connect button is pressed.
    /// </summary>
    public event Action<string, int, string> ConnectButtonPressed;

    /// <summary>
    /// Event that is executed when the start hosting button is pressed.
    /// </summary>
    public event Action<string, int> StartHostButtonPressed;

    public ConnectInterface(
        ModSettings modSettings,
        ComponentGroup connectGroup
        // ComponentGroup settingsGroup
    ) {
        _modSettings = modSettings;

        _connectGroup = connectGroup;
        // _settingsGroup = settingsGroup;

        CreateConnectUi();
    }

    /// <summary>
    /// Callback method for when the client disconnects.
    /// </summary>
    public void OnClientDisconnect() {
        _connectionButton.SetText(ConnectText);
        _connectionButton.SetOnPress(OnConnectButtonPressed);
        _connectionButton.SetInteractable(true);
    }

    /// <summary>
    /// Callback method for when the client successfully connects.
    /// </summary>
    public void OnSuccessfulConnect() {
        // Let the user know that the connection was successful
        SetFeedbackText(Color.green, "Successfully connected");

        // Reset the connection button with the disconnect text
        _connectionButton.SetText(ConnectText);
        _connectionButton.SetInteractable(true);
    }

    /// <summary>
    /// Callback method for when the client fails to connect.
    /// </summary>
    /// <param name="result">The result of the failed connection.</param>
    public void OnFailedConnect(ConnectionFailedResult result) {
        // Let the user know that the connection failed based on the result
        switch (result.Reason) {
            case ConnectionFailedReason.InvalidAddons:
                SetFeedbackText(Color.red, "Failed to connect:\nInvalid addons");
                break;
            case ConnectionFailedReason.SocketException:
            case ConnectionFailedReason.IOException:
                SetFeedbackText(Color.red, "Failed to connect:\nInternal error");
                break;
            case ConnectionFailedReason.TimedOut:
                SetFeedbackText(Color.red, "Failed to connect:\nConnection timed out");
                break;
            case ConnectionFailedReason.Other:
                var message = ((ConnectionFailedMessageResult) result).Message;
                SetFeedbackText(Color.red, $"Failed to connect:\n{message}");
                break;
            default:
                SetFeedbackText(Color.red, $"Failed to connect:\nUnknown reason");
                break;
        }

        // Enable the connect button again
        _connectionButton.SetText(ConnectText);
        _connectionButton.SetInteractable(true);
    }

    /// <summary>
    /// Create the connection UI.
    /// </summary>
    private void CreateConnectUi() {
        // Now we can start adding individual components to our UI
        // Keep track of current x and y of objects we want to place
        var x = 1920f / 2f;
        var y = 1080f - 100f;

        const float labelHeight = 20f;
        const float logoHeight = 74f;

        new ImageComponent(
            _connectGroup,
            new Vector2(x, y),
            new Vector2(240f, logoHeight),
            TextureManager.HkmpLogo
        );

        y -= logoHeight / 2f + 20f;

        new TextComponent(
            _connectGroup,
            new Vector2(x + TextIndentWidth, y),
            new Vector2(212f, labelHeight),
            "Username",
            UiManager.NormalFontSize,
            alignment: TextAnchor.MiddleLeft
        );

        y -= labelHeight + 14f;

        _usernameInput = new InputComponent(
            _connectGroup,
            new Vector2(x, y),
            _modSettings.Username,
            "Username",
            characterLimit: 20,
            onValidateInput: (_, _, addedChar) => char.IsLetterOrDigit(addedChar) ? addedChar : '\0'
        );

        y -= InputComponent.DefaultHeight + 20f;

        new TextComponent(
            _connectGroup,
            new Vector2(x + TextIndentWidth, y),
            new Vector2(212f, labelHeight),
            "Server IP and port",
            UiManager.NormalFontSize,
            alignment: TextAnchor.MiddleLeft
        );

        y -= labelHeight + 14f;

        _addressInput = new IpInputComponent(
            _connectGroup,
            new Vector2(x, y),
            _modSettings.ConnectAddress,
            "IP Address"
        );

        y -= InputComponent.DefaultHeight + 8f;

        var joinPort = _modSettings.ConnectPort;
        _portInput = new PortInputComponent(
            _connectGroup,
            new Vector2(x, y),
            joinPort == -1 ? "" : joinPort.ToString(),
            "Port"
        );

        y -= InputComponent.DefaultHeight + 20f;

        _connectionButton = new ButtonComponent(
            _connectGroup,
            new Vector2(x, y),
            ConnectText
        );
        _connectionButton.SetOnPress(() => OnConnectButtonPressed());

        y -= ButtonComponent.DefaultHeight + 8f;

        _serverButton = new ButtonComponent(
            _connectGroup,
            new Vector2(x, y),
            StartHostingText
        );
        _serverButton.SetOnPress(OnStartButtonPressed);

        y -= ButtonComponent.DefaultHeight + 8f;

        // var settingsButton = new ButtonComponent(
        //     _connectGroup,
        //     new Vector2(x, y),
        //     "Settings"
        // );
        // settingsButton.SetOnPress(() => {
        //     _connectGroup.SetActive(false);
        //     _settingsGroup.SetActive(true);
        // });

        y -= ButtonComponent.DefaultHeight + 8f;

        _feedbackText = new TextComponent(
            _connectGroup,
            new Vector2(x, y),
            new Vector2(240f, labelHeight),
            new Vector2(0.5f, 1f),
            "",
            UiManager.SubTextFontSize,
            alignment: TextAnchor.UpperCenter
        );
        _feedbackText.SetActive(false);
    }

    /// <summary>
    /// Callback method for when the connect button is pressed.
    /// </summary>
    private void OnConnectButtonPressed() {
        var address = _addressInput.GetInput();

        if (address.Length == 0) {
            // Let the user know that the address is empty
            SetFeedbackText(Color.red, "Failed to connect:\nYou must enter an address");

            return;
        }

        var portString = _portInput.GetInput();

        var parsedPort = int.TryParse(portString, out var port);
        if (!parsedPort || port == 0) {
            // Let the user know that the entered port is incorrect
            SetFeedbackText(Color.red, "Failed to connect:\nYou must enter a valid port");

            return;
        }

        Logger.Debug($"Connect button pressed, address: {address}:{port}");

        if (!ValidateUsername(out var username)) {
            return;
        }

        // Input values were valid, so we can store them in the settings
        Logger.Debug($"Saving join address {address} in global settings");
        Logger.Debug($"Saving join port {port} in global settings");
        Logger.Debug($"Saving join username {username} in global settings");
        _modSettings.ConnectAddress = address;
        _modSettings.ConnectPort = port;
        _modSettings.Username = username;

        // Disable the connect button while we are trying to establish a connection
        _connectionButton.SetText(ConnectingText);
        _connectionButton.SetInteractable(false);

        ConnectButtonPressed?.Invoke(address, port, username);
    }

    /// <summary>
    /// Callback method for when the start hosting button is pressed.
    /// </summary>
    private void OnStartButtonPressed() {
        var portString = _portInput.GetInput();

        var parsedPort = int.TryParse(portString, out var port);
        if (!parsedPort || port == 0) {
            // Let the user know that the entered port is incorrect
            SetFeedbackText(Color.red, "Failed to host:\nYou must enter a valid port");

            return;
        }
        
        if (!ValidateUsername(out var username)) {
            return;
        }

        // Start the server in networkManager
        StartHostButtonPressed?.Invoke(username, port);
    }

    /// <summary>
    /// Set the feedback text with the given color and content.
    /// </summary>
    /// <param name="color">The color of the text.</param>
    /// <param name="text">The content of the text.</param>
    private void SetFeedbackText(Color color, string text) {
        _feedbackText.SetColor(color);
        _feedbackText.SetText(text);
        _feedbackText.SetActive(true);

        if (_feedbackHideCoroutine != null) {
            MonoBehaviourUtil.Instance.StopCoroutine(_feedbackHideCoroutine);
        }

        _feedbackHideCoroutine = MonoBehaviourUtil.Instance.StartCoroutine(WaitHideFeedbackText());
    }

    /// <summary>
    /// Coroutine for hiding the feedback text after a delay.
    /// </summary>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator WaitHideFeedbackText() {
        yield return new WaitForSeconds(FeedbackTextHideTime);

        _feedbackText.SetActive(false);
    }

    private bool ValidateUsername(out string username) {
        username = _usernameInput.GetInput();
        if (username.Length == 0 || username.Length > 20) {
            if (username.Length > 20) {
                SetFeedbackText(Color.red, "Failed to connect:\nUsername is too long");
            } else if (username.Length == 0) {
                SetFeedbackText(Color.red, "Failed to connect:\nYou must enter a username");
            }

            return false;
        }

        return true;
    }
}
