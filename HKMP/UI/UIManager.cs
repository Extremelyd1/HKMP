using HKMP.Game;
using HKMP.Networking;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using HKMP.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HKMP.UI {
    public class UIManager {
        private const string PerpetuaFontName = "Perpetua";
        private const string TrajanProName = "TrajanPro-Regular";
        private const string TrajanProBoldName = "TrajanPro-Bold";
        
        private readonly FontManager _fontManager;
        private readonly TextureManager _textureManager;

        private readonly NetworkManager _networkManager;

        private readonly Settings _settings;

        private GameObject _uiObject;

        private IInputComponent _addressInput;
        private IInputComponent _clientPortInput;

        private IInputComponent _usernameInput;

        private IButtonComponent _connectButton;
        private IButtonComponent _disconnectButton;

        private ITextComponent _clientFeedbackText;
        
        private IInputComponent _serverPortInput;

        private IButtonComponent _startButton;
        private IButtonComponent _stopButton;

        private ITextComponent _serverFeedbackText;

        public UIManager(NetworkManager networkManager, Settings settings) {
            _fontManager = new FontManager();
            _textureManager = new TextureManager();

            _networkManager = networkManager;

            _settings = settings;

            _fontManager.LoadFonts();
            _textureManager.LoadTextures();
            
            On.HeroController.Pause += (orig, self) => {
                // Execute original method
                orig(self);

                // Only show UI in non-gameplay scenes
                if (!SceneUtil.IsNonGameplayScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)) {
                    ShowUI();
                }
            };
            On.HeroController.UnPause += (orig, self) => {
                // Execute original method
                orig(self);
                HideUI();
            };
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (oldScene, newScene) => {
                if (newScene.name == "Menu_Title") {
                    HideUI();
                }
            };
        }

        public void CreateUI() {
            // First we create a gameObject that will hold all other objects
            // default to disabling it
            _uiObject = new GameObject();
            _uiObject.SetActive(false);
            
            // Create event system object
            var eventSystemObj = new GameObject("EventSystem");

            var eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystem.sendNavigationEvents = true;
            eventSystem.pixelDragThreshold = 10;

            eventSystemObj.AddComponent<StandaloneInputModule>();

            Object.DontDestroyOnLoad(eventSystemObj);

            // Make sure that our UI is an overlay on the screen
            _uiObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            // Also scale the UI with the screen size
            var canvasScaler = _uiObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            // TODO: check whether the GraphicRaycaster is necessary
            _uiObject.AddComponent<GraphicRaycaster>();

            Object.DontDestroyOnLoad(_uiObject);

            // Now we can start adding individual components to our UI
            // Keep track of current x and y of objects we want to place
            var x = Screen.width - 210.0f;
            var y = Screen.height - 50.0f;

            var multiplayerText = new TextComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Multiplayer",
                _fontManager.GetFont(TrajanProName),
                24
            );

            y -= 35;

            var joinText = new TextComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Join",
                _fontManager.GetFont(TrajanProName),
                18
            );

            y -= 40;

            _addressInput = new InputComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                _settings.JoinAddress,
                "IP Address",
                _textureManager.GetTexture("input_field_background"),
                _fontManager.GetFont(TrajanProName),
                18
            );

            y -= 40;

            var joinPort = _settings.JoinPort;
            _clientPortInput = new InputComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                joinPort == -1 ? "" : joinPort.ToString(),
                "Port",
                _textureManager.GetTexture("input_field_background"),
                _fontManager.GetFont(TrajanProName),
                18
            );

            y -= 40;

            var username = _settings.Username;
            _usernameInput = new InputComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                username,
                "Username",
                _textureManager.GetTexture("input_field_background"),
                _fontManager.GetFont(TrajanProName),
                18
            );

            y -= 40;
            
            _connectButton = new ButtonComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Connect",
                _textureManager.GetTexture("button_background"),
                _fontManager.GetFont(TrajanProName),
                18
            );
            _connectButton.SetOnPress(OnConnectButtonPressed);
            
            _disconnectButton = new ButtonComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Disconnect",
                _textureManager.GetTexture("button_background"),
                _fontManager.GetFont(TrajanProName),
                18
            );
            _disconnectButton.SetOnPress(OnDisconnectButtonPressed);
            _disconnectButton.SetActive(false);

            y -= 40;
            
            _clientFeedbackText = new TextComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "",
                _fontManager.GetFont(TrajanProBoldName),
                15
            );
            _clientFeedbackText.SetActive(false);

            y -= 40;
            
            var hostText = new TextComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Host",
                _fontManager.GetFont(TrajanProName),
                18
            );
            
            y -= 40;

            var hostPort = _settings.HostPort;
            _serverPortInput = new InputComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                hostPort == -1 ? "" : hostPort.ToString(),
                "Port",
                _textureManager.GetTexture("input_field_background"),
                _fontManager.GetFont(TrajanProName),
                18
            );

            y -= 40;
            
            _startButton = new ButtonComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Start",
                _textureManager.GetTexture("button_background"),
                _fontManager.GetFont(TrajanProName),
                18
            );
            _startButton.SetOnPress(OnStartButtonPressed);
            
            _stopButton = new ButtonComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Stop",
                _textureManager.GetTexture("button_background"),
                _fontManager.GetFont(TrajanProName),
                18
            );
            _stopButton.SetOnPress(OnStopButtonPressed);
            _stopButton.SetActive(false);
            
            y -= 40;
            
            _serverFeedbackText = new TextComponent(
                _uiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "",
                _fontManager.GetFont(TrajanProBoldName),
                15
            );
            _serverFeedbackText.SetActive(false);
        }

        private void ShowUI() {
            _uiObject.SetActive(true);
        }

        private void HideUI() {
            _uiObject.SetActive(false);
        }

        public void OnClientDisconnect() {
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
            
            var portString = _clientPortInput.GetInput();
            int port;

            if (!int.TryParse(portString, out port)) {
                // Let the user know that the entered port is incorrect
                _clientFeedbackText.SetColor(Color.red);
                _clientFeedbackText.SetText("Invalid port");
                _clientFeedbackText.SetActive(true);
            
                return;
            }

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
            Logger.Info(this, $"Saving join address {address} in global settings");
            Logger.Info(this, $"Saving join port {port} in global settings");
            Logger.Info(this, $"Saving join username {username} in global settings");
            _settings.JoinAddress = address;
            _settings.JoinPort = port;
            _settings.Username = username;
            
            // Disable the connect button while we are trying to establish a connection
            _connectButton.SetActive(false);

            // Register a callback for when the connection is successful
            _networkManager.RegisterOnConnect(OnSuccessfulConnect);
            _networkManager.RegisterOnConnectFailed(OnFailedConnect);
            _networkManager.ConnectClient(address, port);
        }

        private void OnDisconnectButtonPressed() {
            // Disconnect the client
            _networkManager.DisconnectClient();
            
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
            
            var portString = _serverPortInput.GetInput();
            int port;

            if (!int.TryParse(portString, out port)) {
                // Let the user know that the entered port is incorrect
                _serverFeedbackText.SetColor(Color.red);
                _serverFeedbackText.SetText("Invalid port");
                _serverFeedbackText.SetActive(true);
            
                return;
            }

            // Input value was valid, so we can store it in the settings
            Logger.Info(this, $"Saving host port {port} in global settings");
            _settings.HostPort = port;

            // Start the server in networkManager
            _networkManager.StartServer(port);
            
            // Disable the start button
            _startButton.SetActive(false);
            
            // Enable the stop button
            _stopButton.SetActive(true);
            
            // Let the user know that the server has been started
            _serverFeedbackText.SetColor(Color.green);
            _serverFeedbackText.SetText("Started server");
            _serverFeedbackText.SetActive(true);
        }

        private void OnStopButtonPressed() {
            // Stop the server in networkManager
            _networkManager.StopServer();
            
            // Disable the stop button
            _stopButton.SetActive(false);
            
            // Enable the start button
            _startButton.SetActive(true);
            
            // Let the user know that the server has been stopped
            _serverFeedbackText.SetColor(Color.green);
            _serverFeedbackText.SetText("Stopped server");
            _serverFeedbackText.SetActive(true);
        }

        public string GetEnteredUsername() {
            return _usernameInput.GetInput();
        }
    }
}