using System;
using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using UnityEngine;

namespace Hkmp.Ui {
    public class ConnectInterface {
        private const string LocalhostAddress = "127.0.0.1";

        private const float TextIndentWidth = 5f;

        private const string ConnectText = "Connect";
        private const string DisconnectText = "Disconnect";

        private const string StartHostingText = "Start Hosting";
        private const string StopHostingText = "Stop Hosting";
    
        private readonly ModSettings _modSettings;

        private readonly ComponentGroup _connectGroup;
        private readonly ComponentGroup _settingsGroup;

        private IInputComponent _usernameInput;
        
        private IInputComponent _addressInput;
        private IInputComponent _portInput;

        private IButtonComponent _connectionButton;

        private ITextComponent _clientFeedbackText;

        private IButtonComponent _serverButton;

        private ITextComponent _serverFeedbackText;

        public event Action<string, int, string> ConnectButtonPressed;
        public event Action DisconnectButtonPressed;
        public event Action<int> StartHostButtonPressed;
        public event Action StopHostButtonPressed;

        public ConnectInterface(
            ModSettings modSettings,
            ComponentGroup connectGroup,
            ComponentGroup settingsGroup
        ) {
            _modSettings = modSettings;

            _connectGroup = connectGroup;
            _settingsGroup = settingsGroup;

            CreateConnectUi();
        }

        public void OnClientDisconnect() {
            // Disable the feedback text
            _clientFeedbackText.SetActive(false);

            _connectionButton.SetText(ConnectText);
            _connectionButton.SetOnPress(OnConnectButtonPressed);
            _connectionButton.SetInteractable(true);
        }

        public void OnSuccessfulConnect() {
            // Let the user know that the connection was successful
            _clientFeedbackText.SetColor(Color.green);
            _clientFeedbackText.SetText("Connection success");
            _clientFeedbackText.SetActive(true);

            // Reset the connection button with the disconnect text and callback
            _connectionButton.SetText(DisconnectText);
            _connectionButton.SetOnPress(OnDisconnectButtonPressed);
            _connectionButton.SetInteractable(true);
        }

        public void OnFailedConnect(ConnectFailedResult result) {
            // Let the user know that the connection failed
            _clientFeedbackText.SetColor(Color.red);

            switch (result.Type) {
                case ConnectFailedResult.FailType.InvalidAddons:
                    _clientFeedbackText.SetText("Invalid addons");
                    break;
                case ConnectFailedResult.FailType.InvalidUsername:
                    _clientFeedbackText.SetText("Invalid username");
                    break;
                case ConnectFailedResult.FailType.SocketException:
                    _clientFeedbackText.SetText("Connection failed");
                    break;
                case ConnectFailedResult.FailType.TimedOut:
                    _clientFeedbackText.SetText("Connection timed out");
                    break;
            }
            
            _clientFeedbackText.SetActive(true);

            // Enable the connect button again
            _connectionButton.SetInteractable(true);
        }

        private void CreateConnectUi() {
            // Now we can start adding individual components to our UI
            // Keep track of current x and y of objects we want to place
            var x = 1920f - 210f;
            var y = 1080f - 100f;
            
            const float labelHeight = 20f;
            const float logoHeight = 74f;

            new ImageComponent(
                _connectGroup,
                new Vector2(x, y),
                new Vector2(240, logoHeight),
                TextureManager.HkmpLogo
            );

            y -= logoHeight / 2f + 20f;

            new TextComponent(
                _connectGroup,
                new Vector2(x + TextIndentWidth, y),
                new Vector2(212, labelHeight),
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
                characterLimit: 20
            );

            y -= InputComponent.DefaultHeight + 20f;

            new TextComponent(
                _connectGroup,
                new Vector2(x + TextIndentWidth, y),
                new Vector2(212, labelHeight),
                "Server IP and port",
                UiManager.NormalFontSize,
                alignment: TextAnchor.MiddleLeft
            );

            y -= labelHeight + 14f;

            _addressInput = new IpInputComponent(
                _connectGroup,
                new Vector2(x, y),
                _modSettings.JoinAddress,
                "IP Address"
            );

            y -= InputComponent.DefaultHeight + 8f;

            var joinPort = _modSettings.JoinPort;
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
            _connectionButton.SetOnPress(OnConnectButtonPressed);

            y -= ButtonComponent.DefaultHeight + 8f;

            _serverButton = new ButtonComponent(
                _connectGroup,
                new Vector2(x, y),
                StartHostingText
            );
            _serverButton.SetOnPress(OnStartButtonPressed);

            y -= ButtonComponent.DefaultHeight + 8f;

            var settingsButton = new ButtonComponent(
                _connectGroup,
                new Vector2(x, y),
                "Settings"
            );
            settingsButton.SetOnPress(() => {
                _connectGroup.SetActive(false);
                _settingsGroup.SetActive(true);
            });

            y -= ButtonComponent.DefaultHeight + 8f;

            _clientFeedbackText = new TextComponent(
                _connectGroup,
                new Vector2(x, y),
                new Vector2(212, labelHeight),
                "",
                UiManager.SubTextFontSize
            );
            _clientFeedbackText.SetActive(false);

            y -= labelHeight + 8f;

            _serverFeedbackText = new TextComponent(
                _connectGroup,
                new Vector2(x, y),
                new Vector2(212, labelHeight),
                "",
                UiManager.SubTextFontSize
            );
            _serverFeedbackText.SetActive(false);
        }
        
        private void OnConnectButtonPressed() {
            // Disable feedback text leftover from other actions
            _clientFeedbackText.SetActive(false);

            var address = _addressInput.GetInput();

            if (address.Length == 0) {
                // Let the user know that the address is empty
                _clientFeedbackText.SetColor(Color.red);
                _clientFeedbackText.SetText("Address is empty");
                _clientFeedbackText.SetActive(true);

                return;
            }

            var portString = _portInput.GetInput();

            var parsedPort = int.TryParse(portString, out var port);
            if (!parsedPort || port == 0) {
                // Let the user know that the entered port is incorrect
                _clientFeedbackText.SetColor(Color.red);
                _clientFeedbackText.SetText("Invalid port");
                _clientFeedbackText.SetActive(true);

                return;
            }
            
            Logger.Get().Info(this, $"Connect button pressed, address: {address}:{port}");

            var username = _usernameInput.GetInput();
            if (username.Length == 0 || username.Length > 20) {
                if (username.Length > 20) {
                    _clientFeedbackText.SetText("Username too long");
                } else if (username.Length == 0) {
                    _clientFeedbackText.SetText("Username is empty");
                }

                // Let the user know that the username is too long
                _clientFeedbackText.SetColor(Color.red);
                _clientFeedbackText.SetActive(true);

                return;
            }

            // Input values were valid, so we can store them in the settings
            Logger.Get().Info(this, $"Saving join address {address} in global settings");
            Logger.Get().Info(this, $"Saving join port {port} in global settings");
            Logger.Get().Info(this, $"Saving join username {username} in global settings");
            _modSettings.JoinAddress = address;
            _modSettings.JoinPort = port;
            _modSettings.Username = username;

            // Disable the connect button while we are trying to establish a connection
            _connectionButton.SetInteractable(false);

            ConnectButtonPressed?.Invoke(address, port, username);
        }

        private void OnDisconnectButtonPressed() {
            // Disconnect the client
            DisconnectButtonPressed?.Invoke();

            // Let the user know that the connection was successful
            _clientFeedbackText.SetColor(Color.green);
            _clientFeedbackText.SetText("Disconnect success");
            _clientFeedbackText.SetActive(true);

            _connectionButton.SetText(ConnectText);
            _connectionButton.SetOnPress(OnConnectButtonPressed);
            _connectionButton.SetInteractable(true);
        }

        private void OnStartButtonPressed() {
            // Disable feedback text leftover from other actions
            _clientFeedbackText.SetActive(false);

            var portString = _portInput.GetInput();

            var parsedPort = int.TryParse(portString, out var port);
            if (!parsedPort || port == 0) {
                // Let the user know that the entered port is incorrect
                _serverFeedbackText.SetColor(Color.red);
                _serverFeedbackText.SetText("Invalid port");
                _serverFeedbackText.SetActive(true);

                return;
            }

            // Input value was valid, so we can store it in the settings
            Logger.Get().Info(this, $"Saving host port {port} in global settings");
            _modSettings.HostPort = port;

            // Start the server in networkManager
            StartHostButtonPressed?.Invoke(port);

            _serverButton.SetText(StopHostingText);
            _serverButton.SetOnPress(OnStopButtonPressed);

            // Let the user know that the server has been started
            _serverFeedbackText.SetColor(Color.green);
            _serverFeedbackText.SetText("Started server");
            _serverFeedbackText.SetActive(true);
            
            // If the setting for automatically connecting when hosting is enabled,
            // we connect the client to itself as well
            if (_modSettings.AutoConnectWhenHosting) {
                _addressInput.SetInput(LocalhostAddress);
            
                OnConnectButtonPressed();
            }
        }

        private void OnStopButtonPressed() {
            // Stop the server in networkManager
            StopHostButtonPressed?.Invoke();

            _serverButton.SetText(StartHostingText);
            _serverButton.SetOnPress(OnStartButtonPressed);

            // Let the user know that the server has been stopped
            _serverFeedbackText.SetColor(Color.green);
            _serverFeedbackText.SetText("Stopped server");
            _serverFeedbackText.SetActive(true);
        }
    }
}