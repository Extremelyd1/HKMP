using HKMP.Game.Client;
using HKMP.Game.Server;
using HKMP.Networking.Client;
using HKMP.UI.Component;
using HKMP.UI.Resources;
using HKMP.Util;
using Modding;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ModSettings = HKMP.Game.Settings.ModSettings;

namespace HKMP.UI {
    public class UIManager {
        public static InfoBoxUI InfoBox;

        private readonly ModSettings _modSettings;

        // Whether the pause menu UI is hidden by the keybind
        private bool _isPauseUiHiddenByKeybind;
        // Whether the game is in a state where we normally show the pause menu UI
        // for example in a gameplay scene in the HK pause menu
        private bool _canShowPauseUi;
        
        public UIManager(
            ServerManager serverManager, 
            ClientManager clientManager,
            Game.Settings.GameSettings clientGameSettings,
            Game.Settings.GameSettings serverGameSettings, 
            ModSettings modSettings,
            NetClient netClient
        ) {
            _modSettings = modSettings;
            
            // First we create a gameObject that will hold all other objects of the UI
            var topUiObject = new GameObject();

            // Create event system object
            var eventSystemObj = new GameObject("EventSystem");

            var eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystem.sendNavigationEvents = true;
            eventSystem.pixelDragThreshold = 10;

            eventSystemObj.AddComponent<StandaloneInputModule>();

            Object.DontDestroyOnLoad(eventSystemObj);

            // Make sure that our UI is an overlay on the screen
            topUiObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            // Also scale the UI with the screen size
            var canvasScaler = topUiObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            topUiObject.AddComponent<GraphicRaycaster>();

            Object.DontDestroyOnLoad(topUiObject);

            PrecacheText(topUiObject);

            var pauseMenuUiObject = new GameObject();
            pauseMenuUiObject.transform.SetParent(topUiObject.transform);
            pauseMenuUiObject.SetActive(false);

            var connectUiObject = new GameObject();
            connectUiObject.transform.SetParent(pauseMenuUiObject.transform);

            var clientSettingsUiObject = new GameObject();
            clientSettingsUiObject.transform.SetParent(pauseMenuUiObject.transform);
            var serverSettingsUiObject = new GameObject();
            serverSettingsUiObject.transform.SetParent(pauseMenuUiObject.transform);

            new ConnectUI(
                modSettings,
                clientManager,
                serverManager,
                connectUiObject,
                clientSettingsUiObject,
                serverSettingsUiObject
            );

            new ClientSettingsUI(
                clientGameSettings,
                clientManager,
                clientSettingsUiObject,
                connectUiObject
            );

            new ServerSettingsUI(
                serverGameSettings,
                modSettings,
                serverManager,
                serverSettingsUiObject,
                connectUiObject
            );

            var inGameUiObject = new GameObject();
            inGameUiObject.transform.SetParent(topUiObject.transform);

            var infoBoxUiObject = new GameObject();
            infoBoxUiObject.transform.SetParent(inGameUiObject.transform);

            InfoBox = new InfoBoxUI(infoBoxUiObject);

            var pingUiObject = new GameObject();
            pingUiObject.transform.SetParent(inGameUiObject.transform);

            new PingUI(
                pingUiObject, 
                clientManager, 
                netClient
            );
            
            // Register callbacks to make sure the UI is hidden and shown at correct times
            On.HeroController.Pause += (orig, self) => {
                // Execute original method
                orig(self);

                // Only show UI in gameplay scenes
                if (!SceneUtil.IsNonGameplayScene(SceneUtil.GetCurrentSceneName())) {
                    _canShowPauseUi = true;
                    
                    pauseMenuUiObject.SetActive(!_isPauseUiHiddenByKeybind);
                }
                
                inGameUiObject.SetActive(false);
            };
            On.HeroController.UnPause += (orig, self) => {
                // Execute original method
                orig(self);
                pauseMenuUiObject.SetActive(false);

                _canShowPauseUi = false;
                
                // Only show info box UI in gameplay scenes
                if (!SceneUtil.IsNonGameplayScene(SceneUtil.GetCurrentSceneName())) {
                    inGameUiObject.SetActive(true);
                }
            };
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (oldScene, newScene) => {
                if (SceneUtil.IsNonGameplayScene(newScene.name)) {
                    eventSystem.enabled = false;

                    _canShowPauseUi = false;
                    
                    pauseMenuUiObject.SetActive(false);
                    inGameUiObject.SetActive(false);
                } else {
                    eventSystem.enabled = true;
                    
                    inGameUiObject.SetActive(true);
                }
            };
            
            // The game is automatically unpaused when the knight dies, so we need
            // to disable the UI menu manually
            // TODO: this still gives issues, since it displays the cursor while we are supposed to be unpaused
            ModHooks.Instance.AfterPlayerDeadHook += () => {
                pauseMenuUiObject.SetActive(false);
            };

            MonoBehaviourUtil.Instance.OnUpdateEvent += () => {
                CheckKeybinds(pauseMenuUiObject);
            };
        }

        // TODO: find a more elegant solution to this
        private void PrecacheText(GameObject parent) {
            // Create off-screen text components containing a set of characters we need so they are prerendered,
            // otherwise calculating characterInfo from Unity fails
            new TextComponent(
                parent,
                new Vector2(-10000, 0),
                new Vector2(100, 100),
                StringUtil.AllUsableCharacters,
                FontManager.UIFontRegular,
                13
            );
            new TextComponent(
                parent,
                new Vector2(-10000, 0),
                new Vector2(100, 100),
                StringUtil.AllUsableCharacters,
                FontManager.UIFontRegular,
                18
            );
            new TextComponent(
                parent,
                new Vector2(-10000, 0),
                new Vector2(100, 100),
                StringUtil.AllUsableCharacters,
                FontManager.UIFontBold,
                13
            );
            new TextComponent(
                parent,
                new Vector2(-10000, 0),
                new Vector2(100, 100),
                StringUtil.AllUsableCharacters,
                FontManager.UIFontBold,
                18
            );
        }

        private void CheckKeybinds(GameObject pauseMenuUiObject) {
            if (Input.GetKeyDown((KeyCode) _modSettings.HideUiKey)) {
                _isPauseUiHiddenByKeybind = !_isPauseUiHiddenByKeybind;

                Logger.Info(this, $"Pause UI is now {(_isPauseUiHiddenByKeybind ? "hidden" : "shown")}");

                if (_isPauseUiHiddenByKeybind) {
                    // If we toggled the UI off, we hide it if it was shown
                    pauseMenuUiObject.SetActive(false);
                } else if (_canShowPauseUi) {
                    // If we toggled the UI on again and we are in a pause menu
                    // where we can show the UI, we enabled it
                    pauseMenuUiObject.SetActive(true);
                }
            }
        }
    }
}