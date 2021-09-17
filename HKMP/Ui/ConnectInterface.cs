using Hkmp.Game.Client;
using Hkmp.Game.Server;
using Hkmp.Game.Settings;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace Hkmp.Ui {
    public class ConnectInterface {
        private const string LocalhostAddress = "127.0.0.1";
    
        private readonly ModSettings _modSettings;
        private readonly ClientManager _clientManager;
        private readonly ServerManager _serverManager;

        private readonly ComponentGroup _connectGroup;
        private readonly ComponentGroup _clientSettingsGroup;
        private readonly ComponentGroup _serverSettingsGroup;

        private IInputComponent _addressInput;
        private IInputComponent _portInput;

        private IInputComponent _usernameInput;

        private IButtonComponent _connectButton;
        private IButtonComponent _disconnectButton;

        private ITextComponent _clientFeedbackText;

        private IButtonComponent _startButton;
        private IButtonComponent _stopButton;

        private ITextComponent _serverFeedbackText;

        public ConnectInterface(
            ModSettings modSettings,
            ClientManager clientManager,
            ServerManager serverManager,
            ComponentGroup connectGroup,
            ComponentGroup clientSettingsGroup,
            ComponentGroup serverSettingsGroup
        ) {
            _modSettings = modSettings;
            _clientManager = clientManager;
            _serverManager = serverManager;

            _connectGroup = connectGroup;
            _clientSettingsGroup = clientSettingsGroup;
            _serverSettingsGroup = serverSettingsGroup;

            CreateConnectUi();
        }

        private void CreateConnectUi() {
            // Now we can start adding individual components to our UI
            // Keep track of current x and y of objects we want to place
            var x = 1920f - 210.0f;
            var y = 1080f - 75.0f;

            new TextComponent(
                _connectGroup,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Multiplayer",
                FontManager.UIFontRegular,
                24
            );

            y -= 30;

            new DividerComponent(
                _connectGroup,
                new Vector2(x, y),
                new Vector2(200, 1)
            );

            y -= 30;

            new TextComponent(
                _connectGroup,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Join Server",
                FontManager.UIFontRegular,
                18
            );

            y -= 40;

            _addressInput = new IpInputComponent(
                _connectGroup,
                new Vector2(x, y),
                _modSettings.JoinAddress,
                "IP Address"
            );

            y -= 40;

            var joinPort = _modSettings.JoinPort;
            _portInput = new InputComponent(
                _connectGroup,
                new Vector2(x, y),
                joinPort == -1 ? "" : joinPort.ToString(),
                "Port",
                characterValidation: InputField.CharacterValidation.Integer,
                characterLimit: 5
            );

            y -= 40;

            var username = _modSettings.Username;
            _usernameInput = new InputComponent(
                _connectGroup,
                new Vector2(x, y),
                username,
                "Username",
                characterLimit: 20
            );

            y -= 40;

            var clientSettingsButton = new ButtonComponent(
                _connectGroup,
                new Vector2(x, y),
                "Settings"
            );
            clientSettingsButton.SetOnPress(() => {
                _connectGroup.SetActive(false);
                _clientSettingsGroup.SetActive(true);
            });

            y -= 40;

            _connectButton = new ButtonComponent(
                _connectGroup,
                new Vector2(x, y),
                "Connect"
            );
            _connectButton.SetOnPress(OnConnectButtonPressed);

            _disconnectButton = new ButtonComponent(
                _connectGroup,
                new Vector2(x, y),
                "Disconnect"
            );
            _disconnectButton.SetOnPress(OnDisconnectButtonPressed);
            _disconnectButton.SetActive(false);

            y -= 40;

            _clientFeedbackText = new TextComponent(
                _connectGroup,
                new Vector2(x, y),
                new Vector2(200, 30),
                "",
                FontManager.UIFontBold,
                15
            );
            _clientFeedbackText.SetActive(false);

            y -= 30;

            new DividerComponent(
                _connectGroup,
                new Vector2(x, y),
                new Vector2(200, 1)
            );

            y -= 30;

            new TextComponent(
                _connectGroup,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Host Server",
                FontManager.UIFontRegular,
                18
            );

            y -= 40;

            var serverSettingsButton = new ButtonComponent(
                _connectGroup,
                new Vector2(x, y),
                "Host Settings"
            );
            serverSettingsButton.SetOnPress(() => {
                _connectGroup.SetActive(false);
                _serverSettingsGroup.SetActive(true);
            });

            y -= 40;

            _startButton = new ButtonComponent(
                _connectGroup,
                new Vector2(x, y),
                "Start Hosting"
            );
            _startButton.SetOnPress(OnStartButtonPressed);

            _stopButton = new ButtonComponent(
                _connectGroup,
                new Vector2(x, y),
                "Stop Hosting"
            );
            _stopButton.SetOnPress(OnStopButtonPressed);
            _stopButton.SetActive(false);

            y -= 40;

            _serverFeedbackText = new TextComponent(
                _connectGroup,
                new Vector2(x, y),
                new Vector2(200, 30),
                "",
                FontManager.UIFontBold,
                15
            );
            _serverFeedbackText.SetActive(false);

            // Register a callback for when the connection is successful or failed or disconnects
            _clientManager.RegisterOnDisconnect(OnClientDisconnect);
            _clientManager.RegisterOnConnect(OnSuccessfulConnect);
            _clientManager.RegisterOnConnectFailed(OnFailedConnect);
        }

        private void OnClientDisconnect() {
            // Disable the feedback text
            _clientFeedbackText.SetActive(false);

            // Disable the disconnect button
            _disconnectButton.SetActive(false);

            // Enable the connect button
            _connectButton.SetActive(true);
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
            int port;

            if (!int.TryParse(portString, out port)) {
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
            _connectButton.SetActive(false);

            _clientManager.Connect(address, port, username);
        }

        private void OnDisconnectButtonPressed() {
            // Disconnect the client
            _clientManager.Disconnect();

            // Let the user know that the connection was successful
            _clientFeedbackText.SetColor(Color.green);
            _clientFeedbackText.SetText("Disconnect success");
            _clientFeedbackText.SetActive(true);

            // Disable the disconnect button
            _disconnectButton.SetActive(false);

            // Enable the connect button
            _connectButton.SetActive(true);
        }

        private void OnSuccessfulConnect() {
            // Let the user know that the connection was successful
            _clientFeedbackText.SetColor(Color.green);
            _clientFeedbackText.SetText("Connection success");
            _clientFeedbackText.SetActive(true);

            // Enable the disconnect button
            _disconnectButton.SetActive(true);
        }

        private void OnFailedConnect() {
            // Let the user know that the connection failed
            _clientFeedbackText.SetColor(Color.red);
            _clientFeedbackText.SetText("Connection failed");
            _clientFeedbackText.SetActive(true);

            // Enable the connect button again
            _connectButton.SetActive(true);
        }

        private void OnStartButtonPressed() {
            // Disable feedback text leftover from other actions
            _clientFeedbackText.SetActive(false);

            var portString = _portInput.GetInput();
            int port;

            if (!int.TryParse(portString, out port)) {
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
            _serverManager.Start(port);

            // Disable the start button
            _startButton.SetActive(false);

            // Enable the stop button
            _stopButton.SetActive(true);

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
            _serverManager.Stop();

            // Disable the stop button
            _stopButton.SetActive(false);

            // Enable the start button
            _startButton.SetActive(true);

            // Let the user know that the server has been stopped
            _serverFeedbackText.SetColor(Color.green);
            _serverFeedbackText.SetText("Stopped server");
            _serverFeedbackText.SetActive(true);
        }
    }
}