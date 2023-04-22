using GlobalEnums;
using Hkmp.Api.Client;
using Hkmp.Game.Settings;
using Hkmp.Networking.Client;
using Hkmp.Ui.Chat;
using Hkmp.Util;
using Modding;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Ui;

/// <inheritdoc />
internal class UiManager : IUiManager {
    #region Internal UI manager variables and properties

    /// <summary>
    /// The font size of header text.
    /// </summary>
    public const int HeaderFontSize = 34;

    /// <summary>
    /// The font size of normal text.
    /// </summary>
    public const int NormalFontSize = 24;

    /// <summary>
    /// The font size of the chat text.
    /// </summary>
    public const int ChatFontSize = 22;

    /// <summary>
    /// The font size of sub text.
    /// </summary>
    public const int SubTextFontSize = 22;

    /// <summary>
    /// The global GameObject in which all UI is created.
    /// </summary>
    internal static GameObject UiGameObject;

    /// <summary>
    /// The chat box instance.
    /// </summary>
    internal static ChatBox InternalChatBox;

    /// <summary>
    /// The connect interface.
    /// </summary>
    public ConnectInterface ConnectInterface { get; }

    /// <summary>
    /// The client settings interface.
    /// </summary>
    public ClientSettingsInterface SettingsInterface { get; }

    /// <summary>
    /// The mod settings.
    /// </summary>
    private readonly ModSettings _modSettings;

    /// <summary>
    /// The ping interface.
    /// </summary>
    private readonly PingInterface _pingInterface;

    /// <summary>
    /// Whether the UI is hidden by the key-bind.
    /// </summary>
    private bool _isUiHiddenByKeyBind;

    /// <summary>
    /// Whether the game is in a state where we normally show the pause menu UI for example in a gameplay
    /// scene in the HK pause menu.
    /// </summary>
    private bool _canShowPauseUi;

    #endregion

    #region IUiManager properties

    /// <inheritdoc />
    public IChatBox ChatBox => InternalChatBox;

    #endregion

    public UiManager(
        ServerSettings clientServerSettings,
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

        var uiGroup = new ComponentGroup();

        var pauseMenuGroup = new ComponentGroup(false, uiGroup);

        var connectGroup = new ComponentGroup(parent: pauseMenuGroup);

        var settingsGroup = new ComponentGroup(parent: pauseMenuGroup);

        ConnectInterface = new ConnectInterface(
            modSettings,
            connectGroup,
            settingsGroup
        );

        var inGameGroup = new ComponentGroup(parent: uiGroup);

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
            clientServerSettings,
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

                    pauseMenuGroup.SetActive(!_isUiHiddenByKeyBind);
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

        MonoBehaviourUtil.Instance.OnUpdateEvent += () => { CheckKeyBinds(uiGroup); };
    }

    #region Internal UI manager methods

    /// <summary>
    /// Callback method for when the client successfully connects.
    /// </summary>
    public void OnSuccessfulConnect() {
        ConnectInterface.OnSuccessfulConnect();
        _pingInterface.SetEnabled(true);
        SettingsInterface.OnSuccessfulConnect();
    }

    /// <summary>
    /// Callback method for when the client fails to connect.
    /// </summary>
    /// <param name="result">The result of the failed connection.</param>
    public void OnFailedConnect(ConnectFailedResult result) {
        ConnectInterface.OnFailedConnect(result);
    }

    /// <summary>
    /// Callback method for when the client disconnects.
    /// </summary>
    public void OnClientDisconnect() {
        ConnectInterface.OnClientDisconnect();
        _pingInterface.SetEnabled(false);
        SettingsInterface.OnDisconnect();
    }

    /// <summary>
    /// Callback method for when the team setting in the <see cref="ServerSettings"/> changes.
    /// </summary>
    public void OnTeamSettingChange() {
        SettingsInterface.OnTeamSettingChange();
    }

    /// <summary>
    /// Check key-binds to show/hide the UI.
    /// </summary>
    /// <param name="uiGroup">The component group for the entire UI.</param>
    private void CheckKeyBinds(ComponentGroup uiGroup) {
        if (Input.GetKeyDown((KeyCode) _modSettings.HideUiKey)) {
            // Only allow UI toggling within the pause menu, otherwise the chat input might interfere
            if (_canShowPauseUi) {
                _isUiHiddenByKeyBind = !_isUiHiddenByKeyBind;

                Logger.Debug($"UI is now {(_isUiHiddenByKeyBind ? "hidden" : "shown")}");

                uiGroup.SetActive(!_isUiHiddenByKeyBind);
            }
        }
    }

    #endregion

    #region IUiManager methods

    /// <inheritdoc />
    public void DisableTeamSelection() {
        SettingsInterface.OnAddonSetTeamSelection(false);
    }

    /// <inheritdoc />
    public void EnableTeamSelection() {
        SettingsInterface.OnAddonSetTeamSelection(true);
    }

    /// <inheritdoc />
    public void DisableSkinSelection() {
        SettingsInterface.OnAddonSetSkinSelection(false);
    }

    /// <inheritdoc />
    public void EnableSkinSelection() {
        SettingsInterface.OnAddonSetSkinSelection(true);
    }

    #endregion
}
