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
        public static InfoBoxUI InfoBox;
        
        public UIManager(ServerManager serverManager, ClientManager clientManager,
            Game.Settings.GameSettings gameSettings, ModSettings modSettings) {
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
            var settingsUiObject = new GameObject();
            settingsUiObject.transform.SetParent(pauseMenuUiObject.transform);

            new ConnectUI(
                modSettings,
                clientManager,
                serverManager,
                connectUiObject,
                settingsUiObject
            );

            new SettingsUI(
                gameSettings,
                modSettings,
                serverManager,
                settingsUiObject,
                connectUiObject
            );

            var inGameUiObject = new GameObject();
            inGameUiObject.transform.SetParent(topUiObject.transform);

            var infoBoxUiObject = new GameObject();
            infoBoxUiObject.transform.SetParent(inGameUiObject.transform);

            InfoBox = new InfoBoxUI(infoBoxUiObject);
            
            // Register callbacks to make sure the UI is hidden and shown at correct times
            On.HeroController.Pause += (orig, self) => {
                // Execute original method
                orig(self);

                // Only show UI in gameplay scenes
                if (!SceneUtil.IsNonGameplayScene(SceneUtil.GetCurrentSceneName())) {
                    pauseMenuUiObject.SetActive(true);
                }
                
                infoBoxUiObject.SetActive(false);
            };
            On.HeroController.UnPause += (orig, self) => {
                // Execute original method
                orig(self);
                pauseMenuUiObject.SetActive(false);
                
                // Only show info box UI in gameplay scenes
                if (!SceneUtil.IsNonGameplayScene(SceneUtil.GetCurrentSceneName())) {
                    infoBoxUiObject.SetActive(true);
                }
            };
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (oldScene, newScene) => {
                if (SceneUtil.IsNonGameplayScene(newScene.name)) {
                    eventSystem.enabled = false;
                    
                    pauseMenuUiObject.SetActive(false);
                    infoBoxUiObject.SetActive(false);
                } else {
                    eventSystem.enabled = true;
                    
                    infoBoxUiObject.SetActive(true);
                }
            };
            
            // The game is automatically unpaused when the knight dies, so we need
            // to disable the UI menu manually
            // TODO: this still gives issues, since it displays the cursor while we are supposed to be unpaused
            ModHooks.Instance.AfterPlayerDeadHook += () => {
                pauseMenuUiObject.SetActive(false);
            };
        }

        // TODO: find a more elegant solution to this
        private void PrecacheText(GameObject parent) {
            // Create off-screen text components containing a set of characters we need so they are prerendered,
            // otherwise calculating characterInfo from Unity fails
            new TextComponent(
                parent,
                new Vector2(-100, 0),
                new Vector2(100, 100),
                StringUtil.AllUsableCharacters,
                FontManager.UIFontRegular,
                13
            );
            new TextComponent(
                parent,
                new Vector2(-100, 0),
                new Vector2(100, 100),
                StringUtil.AllUsableCharacters,
                FontManager.UIFontRegular,
                18
            );
            new TextComponent(
                parent,
                new Vector2(-100, 0),
                new Vector2(100, 100),
                StringUtil.AllUsableCharacters,
                FontManager.UIFontBold,
                13
            );
            new TextComponent(
                parent,
                new Vector2(-100, 0),
                new Vector2(100, 100),
                StringUtil.AllUsableCharacters,
                FontManager.UIFontBold,
                18
            );
        }
    }
}