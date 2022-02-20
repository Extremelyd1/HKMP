using GlobalEnums;
using Hkmp.Api.Client;
using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Ui.Chat;
using Hkmp.Ui.Component;
using Hkmp.Ui.Resources;
using Hkmp.Util;
using Modding;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hkmp.Ui {
    public class UiManager : IUiManager {
        #region Internal UI manager variables and properties

        public const int HeaderFontSize = 34;
        public const int NormalFontSize = 24;
        public const int ChatFontSize = 22;
        public const int SubTextFontSize = 22;
        
        public static GameObject UiGameObject;

        public static ChatBox InternalChatBox;
        
        public ConnectInterface ConnectInterface { get; }
        public ClientSettingsInterface SettingsInterface { get; }

        private readonly ModSettings _modSettings;
        
        private readonly PingInterface _pingInterface;

        // Whether the pause menu UI is hidden by the keybind
        private bool _isPauseUiHiddenByKeybind;

        // Whether the game is in a state where we normally show the pause menu UI
        // for example in a gameplay scene in the HK pause menu
        private bool _canShowPauseUi;
        
        #endregion

        #region IUiManager properties

        public IChatBox ChatBox => InternalChatBox;

        #endregion

        public UiManager(
            Game.Settings.GameSettings clientGameSettings,
            ModSettings modSettings,
            NetClient netClient
        ) {
            _modSettings = modSettings;

            // First we create a gameObject that will hold all other objects of the UI
            UiGameObject = new GameObject();

            // Create event system object
            var eventSystemObj = new GameObject("EventSystem");

            var eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystem.sendNavigationEvents = true;
            eventSystem.pixelDragThreshold = 10;

            eventSystemObj.AddComponent<StandaloneInputModule>();

            Object.DontDestroyOnLoad(eventSystemObj);

            // Make sure that our UI is an overlay on the screen
            UiGameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            // Also scale the UI with the screen size
            var canvasScaler = UiGameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);

            UiGameObject.AddComponent<GraphicRaycaster>();

            Object.DontDestroyOnLoad(UiGameObject);

            PrecacheText();

            var pauseMenuGroup = new ComponentGroup(false);

            var connectGroup = new ComponentGroup(parent: pauseMenuGroup);

            var settingsGroup = new ComponentGroup(parent: pauseMenuGroup);

            ConnectInterface = new ConnectInterface(
                modSettings,
                connectGroup,
                settingsGroup
            );

            var inGameGroup = new ComponentGroup();

            var infoBoxGroup = new ComponentGroup(parent: inGameGroup);

            InternalChatBox = new ChatBox(infoBoxGroup, modSettings);

            var pingGroup = new ComponentGroup(parent: inGameGroup);

            _pingInterface = new PingInterface(
                pingGroup,
                modSettings,
                netClient
            );

            SettingsInterface = new ClientSettingsInterface(
                modSettings,
                clientGameSettings,
                settingsGroup,
                connectGroup,
                _pingInterface
            );

            // Register callbacks to make sure the UI is hidden and shown at correct times
            On.UIManager.SetState += (orig, self, state) => {
                orig(self, state);

                if (state == UIState.PAUSED) {
                    // Only show UI in gameplay scenes
                    if (!SceneUtil.IsNonGameplayScene(SceneUtil.GetCurrentSceneName())) {
                        _canShowPauseUi = true;

                        pauseMenuGroup.SetActive(!_isPauseUiHiddenByKeybind);
                    }

                    inGameGroup.SetActive(false);
                } else {
                    pauseMenuGroup.SetActive(false);

                    _canShowPauseUi = false;

                    // Only show chat box UI in gameplay scenes
                    if (!SceneUtil.IsNonGameplayScene(SceneUtil.GetCurrentSceneName())) {
                        inGameGroup.SetActive(true);
                    }
                }
            };
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (oldScene, newScene) => {
                if (SceneUtil.IsNonGameplayScene(newScene.name)) {
                    eventSystem.enabled = false;

                    _canShowPauseUi = false;

                    pauseMenuGroup.SetActive(false);
                    inGameGroup.SetActive(false);
                } else {
                    eventSystem.enabled = true;

                    inGameGroup.SetActive(true);
                }
            };

            // The game is automatically unpaused when the knight dies, so we need
            // to disable the UI menu manually
            // TODO: this still gives issues, since it displays the cursor while we are supposed to be unpaused
            ModHooks.AfterPlayerDeadHook += () => { pauseMenuGroup.SetActive(false); };
            
            MonoBehaviourUtil.Instance.OnUpdateEvent += () => { CheckKeyBinds(pauseMenuGroup); };
        }

        #region Internal UI manager methods

        public void OnSuccessfulConnect() {
            ConnectInterface.OnSuccessfulConnect();
            _pingInterface.SetEnabled(true);
            SettingsInterface.OnSuccessfulConnect();
        }

        public void OnFailedConnect(ConnectFailedResult result) {
            ConnectInterface.OnFailedConnect(result);
        }

        public void OnClientDisconnect() {
            ConnectInterface.OnClientDisconnect();
            _pingInterface.SetEnabled(false);
            SettingsInterface.OnDisconnect();
        }

        public void OnTeamSettingChange() {
            SettingsInterface.OnTeamSettingChange();
        }

        // TODO: find a more elegant solution to this
        private void PrecacheText() {
            // Create off-screen text components containing a set of characters we need so they are prerendered,
            // otherwise calculating text width from Unity fails and crashes the game
            var fontSizes = new[] {NormalFontSize, ChatFontSize};

            foreach (var fontSize in fontSizes) {
                new TextComponent(
                    null,
                    new Vector2(-10000, 0),
                    new Vector2(100, 100),
                    StringUtil.AllUsableCharacters,
                    fontSize
                );
                new TextComponent(
                    null,
                    new Vector2(-10000, 0),
                    new Vector2(100, 100),
                    StringUtil.AllUsableCharacters,
                    fontSize
                );
            }
        }

        private void CheckKeyBinds(ComponentGroup pauseMenuGroup) {
            if (Input.GetKeyDown((KeyCode) _modSettings.HideUiKey)) {
                _isPauseUiHiddenByKeybind = !_isPauseUiHiddenByKeybind;

                Logger.Get().Info(this, $"Pause UI is now {(_isPauseUiHiddenByKeybind ? "hidden" : "shown")}");

                if (_isPauseUiHiddenByKeybind) {
                    // If we toggled the UI off, we hide it if it was shown
                    pauseMenuGroup.SetActive(false);
                } else if (_canShowPauseUi) {
                    // If we toggled the UI on again and we are in a pause menu
                    // where we can show the UI, we enabled it
                    pauseMenuGroup.SetActive(true);
                }
            }
        }
        
        #endregion

        #region IUiManager methods

        public void DisableTeamSelection() {
            SettingsInterface.OnAddonSetTeamSelection(false);
        }

        public void EnableTeamSelection() {
            SettingsInterface.OnAddonSetTeamSelection(true);
        }

        public void DisableSkinSelection() {
            SettingsInterface.OnAddonSetSkinSelection(false);
        }

        public void EnableSkinSelection() {
            SettingsInterface.OnAddonSetSkinSelection(true);
        }

        #endregion
    }
}