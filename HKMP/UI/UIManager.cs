using System.Collections.Generic;
using HKMP.Game;
using HKMP.Game.Server;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using HKMP.Util;
using Modding;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ModSettings = HKMP.Game.Settings.ModSettings;
using Object = UnityEngine.Object;

namespace HKMP.UI {
    public class UIManager {
        private SettingsEntry[] _settingsEntries;

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

        private int _currentPage = 1;

        public UIManager(ServerManager serverManager, ClientManager clientManager,
            Game.Settings.GameSettings gameSettings, ModSettings modSettings) {
            _serverManager = serverManager;
            _clientManager = clientManager;
            _gameSettings = gameSettings;

            _modSettings = modSettings;

            // Create the settings
            CreateSettings();

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
                if (SceneUtil.IsNonGameplayScene(newScene.name)) {
                    _topUiObject.SetActive(false);
                }
            };
            
            // The game is automatically unpaused when the knight dies, so we need
            // to disable the UI menu manually
            // TODO: this still gives issues, since it displays the cursor while we are supposed to be unpaused
            ModHooks.Instance.AfterPlayerDeadHook += () => {
                _topUiObject.SetActive(false);
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
                FontManager.UIFontRegular,
                24
            );

            y -= 35;

            var joinText = new TextComponent(
                _connectUiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Join",
                FontManager.UIFontRegular,
                18
            );

            y -= 40;

            _addressInput = new HiddenInputComponent(
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
                FontManager.UIFontBold,
                15
            );
            _clientFeedbackText.SetActive(false);

            y -= 40;

            var hostText = new TextComponent(
                _connectUiObject,
                new Vector2(x, y),
                new Vector2(200, 30),
                "Host",
                FontManager.UIFontRegular,
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
                FontManager.UIFontBold,
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

            const float pageYLimit = 250;
            
            var x = Screen.width - 210.0f;
            var y = pageYLimit;

            const int boolMargin = 75;
            const int doubleBoolMargin = 100;
            const int intMargin = 100;
            const int doubleIntMargin = 125;

            var settingsUIEntries = new List<SettingsUIEntry>();
            var pages = new Dictionary<int, GameObject>();

            var currentPage = 0;
            GameObject currentPageObject = null;

            foreach (var settingsEntry in _settingsEntries) {
                if (y <= pageYLimit) {
                    currentPage++;
                    currentPageObject = new GameObject($"Settings Page {currentPage}");
                    currentPageObject.SetActive(currentPage == 1);
                    currentPageObject.transform.SetParent(_settingsUiObject.transform);
                    
                    pages.Add(currentPage, currentPageObject);

                    y = Screen.height - 50.0f;
                }
                
                var nameChars = settingsEntry.Name.ToCharArray();
                var font = FontManager.UIFontRegular;

                var nameWidth = 0;
                foreach (var nameChar in nameChars) {
                    font.GetCharacterInfo(nameChar, out var characterInfo, 18);
                    nameWidth += characterInfo.advance;
                }

                var doubleLine = nameWidth >= SettingsUIEntry.TextWidth;

                settingsUIEntries.Add(new SettingsUIEntry(
                    currentPageObject,
                    new Vector2(x, y),
                    settingsEntry.Name,
                    settingsEntry.Type,
                    settingsEntry.DefaultValue,
                    settingsEntry.InitialValue,
                    settingsEntry.ApplySetting,
                    doubleLine
                ));

                if (doubleLine) {
                    y -= settingsEntry.Type == typeof(bool) ? doubleBoolMargin : doubleIntMargin;
                } else {
                    y -= settingsEntry.Type == typeof(bool) ? boolMargin : intMargin;
                }
            }

            y = pageYLimit - 80;

            var nextPageButton = new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Next page"
            );
            nextPageButton.SetOnPress(() => {
                // Disable old current page
                pages[_currentPage].SetActive(false);
                
                // Increment page if we can
                if (_currentPage < pages.Count) {
                    _currentPage++;
                }

                // Enable new current page
                pages[_currentPage].SetActive(true);
            });

            y -= 40;
            
            var previousPageButton = new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Previous page"
            );
            previousPageButton.SetOnPress(() => {
                // Disable old current page
                pages[_currentPage].SetActive(false);
                
                // Decrement page if we can
                if (_currentPage > 1) {
                    _currentPage--;
                }

                // Enable new current page
                pages[_currentPage].SetActive(true);
            });

            y -= 40;

            var saveSettingsButton = new ButtonComponent(
                _settingsUiObject,
                new Vector2(x, y),
                "Save settings"
            );
            saveSettingsButton.SetOnPress(() => {
                // TODO: check if there are actually changes, otherwise this button will
                // bombard clients with packets
                foreach (var settingsUIEntry in settingsUIEntries) {
                    settingsUIEntry.ApplySetting();
                }

                _modSettings.GameSettings = _gameSettings;

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

        private void CreateSettings() {
            _settingsEntries = new[] {
                new SettingsEntry(
                    "Is PvP Enabled", 
                    typeof(bool),
                    false,
                    _gameSettings.IsPvpEnabled, 
                    o => _gameSettings.IsPvpEnabled = (bool) o
                ),
                new SettingsEntry(
                    "Is body damage enabled", 
                    typeof(bool), 
                    true,
                    _gameSettings.IsBodyDamageEnabled, 
                    o => _gameSettings.IsBodyDamageEnabled = (bool) o
                ),
                new SettingsEntry(
                    "Always show map locations", 
                    typeof(bool), 
                    false,
                    _gameSettings.AlwaysShowMapIcons, 
                    o => _gameSettings.AlwaysShowMapIcons = (bool) o
                ),
                new SettingsEntry(
                    "Only broadcast map with Wayward Compass", 
                    typeof(bool), 
                    true,
                    _gameSettings.OnlyBroadcastMapIconWithWaywardCompass, 
                    o => _gameSettings.OnlyBroadcastMapIconWithWaywardCompass = (bool) o
                ),
                new SettingsEntry(
                    "Display names above players",
                    typeof(bool),
                    true,
                    _gameSettings.DisplayNames,
                    o => _gameSettings.DisplayNames = (bool) o
                ),
                new SettingsEntry(
                    "Nail damage",
                    typeof(int),
                    1,
                    _gameSettings.NailDamage,
                    o => _gameSettings.NailDamage = (int) o
                ),
                new SettingsEntry(
                    "Vengeful Spirit damage",
                    typeof(int),
                    1,
                    _gameSettings.VengefulSpiritDamage,
                    o => _gameSettings.VengefulSpiritDamage = (int) o
                ),
                new SettingsEntry(
                    "Shade Soul damage",
                    typeof(int),
                    2,
                    _gameSettings.ShadeSoulDamage,
                    o => _gameSettings.ShadeSoulDamage = (int) o
                ),
                new SettingsEntry(
                    "Desolate Dive damage",
                    typeof(int),
                    1,
                    _gameSettings.DesolateDiveDamage,
                    o => _gameSettings.DesolateDiveDamage = (int) o
                ),
                new SettingsEntry(
                    "Descending Dark damage",
                    typeof(int),
                    2,
                    _gameSettings.DescendingDarkDamage,
                    o => _gameSettings.DescendingDarkDamage = (int) o
                ),
                new SettingsEntry(
                    "Howling Wraiths damage",
                    typeof(int),
                    1,
                    _gameSettings.HowlingWraithDamage,
                    o => _gameSettings.HowlingWraithDamage = (int) o
                ),
                new SettingsEntry(
                    "Abyss Shriek damage",
                    typeof(int),
                    2,
                    _gameSettings.AbyssShriekDamage,
                    o => _gameSettings.AbyssShriekDamage = (int) o
                ),
                new SettingsEntry(
                    "Great Slash damage",
                    typeof(int),
                    2,
                    _gameSettings.GreatSlashDamage,
                    o => _gameSettings.GreatSlashDamage = (int) o
                ),
                new SettingsEntry(
                    "Dash Slash damage",
                    typeof(int),
                    2,
                    _gameSettings.DashSlashDamage,
                    o => _gameSettings.DashSlashDamage = (int) o
                ),
                new SettingsEntry(
                    "Cyclone Slash damage",
                    typeof(int),
                    1,
                    _gameSettings.CycloneSlashDamage,
                    o => _gameSettings.CycloneSlashDamage = (int) o
                ),
            };
        }
    }
}