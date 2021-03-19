using HKMP.Game;
using HKMP.Game.Server;
using HKMP.Util;
using Modding;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ModSettings = HKMP.Game.Settings.ModSettings;
using Object = UnityEngine.Object;

namespace HKMP.UI {
    public class UIManager {
        public UIManager(ServerManager serverManager, ClientManager clientManager,
            Game.Settings.GameSettings gameSettings, ModSettings modSettings) {
            // First we create a gameObject that will hold all other objects of the UI
            var topUiObject = new GameObject();
            // It's also inactive until we open the pause menu
            topUiObject.SetActive(false);

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

            var connectUiObject = new GameObject();
            connectUiObject.transform.SetParent(topUiObject.transform);
            var settingsUiObject = new GameObject();
            settingsUiObject.transform.SetParent(topUiObject.transform);

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
            
            // Register callbacks to make sure the UI is hidden and shown at correct times
            On.HeroController.Pause += (orig, self) => {
                // Execute original method
                orig(self);

                // Only show UI in non-gameplay scenes
                if (!SceneUtil.IsNonGameplayScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name)) {
                    topUiObject.SetActive(true);
                }
            };
            On.HeroController.UnPause += (orig, self) => {
                // Execute original method
                orig(self);
                topUiObject.SetActive(false);
            };
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (oldScene, newScene) => {
                if (SceneUtil.IsNonGameplayScene(newScene.name)) {
                    topUiObject.SetActive(false);
                }
            };
            
            // The game is automatically unpaused when the knight dies, so we need
            // to disable the UI menu manually
            // TODO: this still gives issues, since it displays the cursor while we are supposed to be unpaused
            ModHooks.Instance.AfterPlayerDeadHook += () => {
                topUiObject.SetActive(false);
            };
        }
    }
}