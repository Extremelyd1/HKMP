using HKMP.Game;
using HKMP.Game.Server;
using HKMP.Networking;
using HKMP.Networking.Packet.Custom;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using HKMP.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HKMP.UI {
    public class UIManager {
        public const string PerpetuaFontName = "Perpetua";
        public const string TrajanProName = "TrajanPro-Regular";
        public const string TrajanProBoldName = "TrajanPro-Bold";
        
        private readonly ServerManager _serverManager;
        private readonly ClientManager _clientManager;
        private readonly Game.Settings.GameSettings _gameSettings;

        private readonly ModSettings _modSettings;

        private GameObject _topUiObject;
        
        private GameObject _connectUiObject;

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

        private GameObject _settingsUiObject;

        public UIManager(ServerManager serverManager, ClientManager clientManager, Game.Settings.GameSettings gameSettings, ModSettings modSettings) {
            _serverManager = serverManager;
            _clientManager = clientManager;
            _gameSettings = gameSettings;

            _modSettings = modSettings;

            // Load necessary resources for the UI
            FontManager.LoadFonts();
            TextureManager.LoadTextures();
            
            // Register a callback when the client disconnects, so we can update the UI
            _clientManager.RegisterOnDisconnect(OnClientDisconnect);
            
            // Register callbacks to make sure the UI is hidden and shown at correct times
            On.HeroController.Pause += (orig, self) => {
                // Execute original method
                orig(self);

                // Only show UI in non-gameplay scenes
                if (!SceneUtil.IsNonGameplayScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)) {
                    _topUiObject.SetActive(true);
                }
            };
            On.HeroController.UnPause += (orig, self) => {
                // Execute original method
                orig(self);
                _topUiObject.SetActive(false);
            };
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (oldScene, newScene) => {
                if (newScene.name == "Menu_Title") {
                    _topUiObject.SetActive(false);
                }
            };
        }

        public void CreateUI() {
            // First we create a gameObject that will hold all other objects
            // default to disabling it
            _topUiObject = new GameObject();
            _topUiObject.SetActive(false);
            
            // Create event system object
            var eventSystemObj = new GameObject("EventSystem");

            var eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystem.sendNavigationEvents = true;
            eventSystem.pixelDragThreshold = 10;

            eventSystemObj.AddComponent<StandaloneInputModule>();

            Object.DontDestroyOnLoad(eventSystemObj);

            // Make sure that our UI is an overlay on the screen
            _topUiObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            // Also scale the UI with the screen size
            var canvasScaler = _topUiObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            // TODO: check whether the GraphicRaycaster is necessary
            _topUiObject.AddComponent<GraphicRaycaster>();

            Object.DontDestroyOnLoad(_topUiObject);
            
            CreateConnectUI(_topUiObject);
            CreateSettingsUI(_topUiObject);
        }

        private void CreateConnectUI(GameObject parent) {
            _connectUiObject = new GameObject();
            _connectUiObject.transform.SetParent(parent.transform);

            // Now we can start adding individual components to our UI
            // Keep track of current x and y of objects we want to place
            var x = Screen.width - 210.0f;
            var y = Screen.height - 50.0f;

            var multiplayerText = new TextComponent(
                _connectUiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Multiplayer",
                FontManager.GetFont(TrajanProName),
                24
            );

            y -= 35;

            var joinText = new TextComponent(
                _connectUiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Join",
                FontManager.GetFont(TrajanProName),
                18
            );

            y -= 40;

            _addressInput = new InputComponent(
                _connectUiObject,
                new Vector2(x, y),
                _modSettings.JoinAddress,
                "IP Address"
            );

            y -= 40;

            var joinPort = _modSettings.JoinPort;
            _clientPortInput = new InputComponent(
                _connectUiObject,
                new Vector2(x, y),
                joinPort == -1 ? "" : joinPort.ToString(),
                "Port",
                characterValidation: InputField.CharacterValidation.Integer
            );

            y -= 40;

            var username = _modSettings.Username;
            _usernameInput = new InputComponent(
                _connectUiObject,
                new Vector2(x, y),
                username,
                "Username"
            );

            y -= 40;
            
            _connectButton = new ButtonComponent(
                _connectUiObject,
                new Vector2(x, y),
                "Connect"
            );
            _connectButton.SetOnPress(OnConnectButtonPressed);
            
            _disconnectButton = new ButtonComponent(
                _connectUiObject,
                new Vector2(x, y),
                "Disconnect"
            );
            _disconnectButton.SetOnPress(OnDisconnectButtonPressed);
            _disconnectButton.SetActive(false);

            y -= 40;
            
            _clientFeedbackText = new TextComponent(
                _connectUiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "",
                FontManager.GetFont(TrajanProBoldName),
                15
            );
            _clientFeedbackText.SetActive(false);

            y -= 40;
            
            var hostText = new TextComponent(
                _connectUiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Host",
                FontManager.GetFont(TrajanProName),
                18
            );
            
            y -= 40;

            var hostPort = _modSettings.HostPort;
            _serverPortInput = new InputComponent(
                _connectUiObject,
                new Vector2(x, y),
                hostPort == -1 ? "" : hostPort.ToString(),
                "Port",
                characterValidation: InputField.CharacterValidation.Integer
            );

            y -= 40;
            
            _startButton = new ButtonComponent(
                _connectUiObject,
                new Vector2(x, y),
                "Start"
            );
            _startButton.SetOnPress(OnStartButtonPressed);
            
            _stopButton = new ButtonComponent(
                _connectUiObject,
                new Vector2(x, y),
                "Stop"
            );
            _stopButton.SetOnPress(OnStopButtonPressed);
            _stopButton.SetActive(false);
            
            y -= 40;
            
            _serverFeedbackText = new TextComponent(
                _connectUiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "",
                FontManager.GetFont(TrajanProBoldName),
                15
            );
            _serverFeedbackText.SetActive(false);

            y -= 40;

            new ButtonComponent(
                _connectUiObject,
                new Vector2(x, y),
                "Settings"
            ).SetOnPress(() => {
                _connectUiObject.SetActive(false);
                _settingsUiObject.SetActive(true);
            });
        }

        private void CreateSettingsUI(GameObject parent) {
            _settingsUiObject = new GameObject();
            _settingsUiObject.transform.SetParent(parent.transform);
            _settingsUiObject.SetActive(false);

            var x = Screen.width - 210.0f;
            var y = Screen.height - 50.0f;

            const int boolMargin = 75;
            // const int doubleBoolMargin = 100;
            // const int intMargin = 100;
            // const int doubleIntMargin = 125;

            var pvpEntry = new SettingsEntry<bool>(
                _settingsUiObject,
                new Vector2(x, y),
                "Enable PvP",
                false
            );
            
            y -= boolMargin;

            var bodyDamageEntry = new SettingsEntry<bool>(
                _settingsUiObject,
                new Vector2(x, y),
                "Enable body damage",
                true
            );

            y -= boolMargin;

            var saveSettingsButton = new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Save settings"
            );
            saveSettingsButton.SetOnPress(() => {
                // TODO: check if there are actually changes, otherwise this button will
                // bombard clients with packets

                _gameSettings.IsPvpEnabled = pvpEntry.GetValue();
                _gameSettings.IsBodyDamageEnabled = bodyDamageEntry.GetValue();
                
                _serverManager.OnUpdateGameSettings();
            });

            y -= 40;

            new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Back"
            ).SetOnPress(() => {
                _settingsUiObject.SetActive(false);
                _connectUiObject.SetActive(true);
            });
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
            _modSettings.JoinAddress = address;
            _modSettings.JoinPort = port;
            _modSettings.Username = username;
            
            // Disable the connect button while we are trying to establish a connection
            _connectButton.SetActive(false);

            // Register a callback for when the connection is successful or failed
            _clientManager.RegisterOnConnect(OnSuccessfulConnect);
            _clientManager.RegisterOnConnectFailed(OnFailedConnect);
            
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