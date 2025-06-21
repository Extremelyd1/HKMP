using System;
using System.Collections;
using System.Collections.Generic;
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
using Object = UnityEngine.Object;

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
    /// The address to connect to the local device.
    /// </summary>
    private const string LocalhostAddress = "127.0.0.1";

    /// <summary>
    /// Expression for the GameManager instance.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static GameManager GM => GameManager.instance;
    
    /// <summary>
    /// Expression for the UIManager instance.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static UIManager UM => UIManager.instance;
    
    /// <summary>
    /// Expression for the InputHandler instance.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    private static InputHandler IH => InputHandler.Instance;

    /// <summary>
    /// The global GameObject in which all UI is created.
    /// </summary>
    internal static GameObject UiGameObject;

    /// <summary>
    /// The chat box instance.
    /// </summary>
    internal static ChatBox InternalChatBox;
    
    /// <summary>
    /// Event that is fired when a server is requested to be hosted from the UI.
    /// </summary>
    public event Action<int> RequestServerStartHostEvent;

    /// <summary>
    /// Event that is fired when a server is requested to be stopped.
    /// </summary>
    public event Action RequestServerStopHostEvent;

    /// <summary>
    /// Event that is fired when a connection is requested with the given username, IP, port and whether it was a
    /// connection from hosting.
    /// </summary>
    public event Action<string, int, string, bool> RequestClientConnectEvent;

    /// <summary>
    /// Event that is fired when a disconnect is requested.
    /// </summary>
    public event Action RequestClientDisconnectEvent;

    /// <summary>
    /// The connect interface.
    /// </summary>
    private readonly ConnectInterface _connectInterface;
    
    /// <summary>
    /// The ping interface.
    /// </summary>
    private readonly PingInterface _pingInterface;

    private readonly ComponentGroup _pauseMenuGroup;

    private readonly GameObject _backButtonObj;
    
    private List<EventTrigger.Entry> _originalBackTriggers;

    /// <summary>
    /// Callback action to execute when save slot selection is finished. The boolean parameter indicates whether a
    /// save slot was selected (true) or the menu was exited through the back button (false). 
    /// </summary>
    private Action<bool> _saveSlotSelectedAction;

    #endregion

    #region IUiManager properties

    /// <inheritdoc />
    public IChatBox ChatBox => InternalChatBox;

    #endregion

    public UiManager(
        ModSettings modSettings,
        NetClient netClient
    ) {
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
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        UiGameObject.AddComponent<GraphicRaycaster>();

        Object.DontDestroyOnLoad(UiGameObject);

        var uiGroup = new ComponentGroup();

        var pauseMenuGroup = new ComponentGroup(false, uiGroup);
        _pauseMenuGroup = pauseMenuGroup;

        var connectGroup = new ComponentGroup(parent: pauseMenuGroup);

        _connectInterface = new ConnectInterface(
            modSettings,
            connectGroup
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

        // Register callbacks to make sure the UI is hidden and shown at correct times
        On.UIManager.SetState += (orig, self, state) => {
            orig(self, state);

            if (state == UIState.PAUSED) {
                inGameGroup.SetActive(false);
            } else {
                // Only show chat box UI in gameplay scenes
                if (!SceneUtil.IsNonGameplayScene(SceneUtil.GetCurrentSceneName())) {
                    inGameGroup.SetActive(true);
                }
            }
        };
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (_, newScene) => {
            var isNonGamePlayScene = SceneUtil.IsNonGameplayScene(newScene.name);
            
            eventSystem.enabled = !isNonGamePlayScene;
            inGameGroup.SetActive(!isNonGamePlayScene);
        };

        ModHooks.LanguageGetHook += (key, sheet, orig) => {
            if (key == "StartMultiplayerBtn" && sheet == "MainMenu") {
                return "Start Multiplayer";
            }
            
            if (key == "MODAL_PROGRESS" && sheet == "MainMenu" && netClient.IsConnected) {
                return "You will be disconnected";
            }

            return orig;
        };
        
        On.UIManager.UIGoToMainMenu += (orig, self) => {
            orig(self);

            TryAddMultiOption();
        };
        
        TryAddMultiOption();

        var achievementsMenuControls = UM.achievementsMenuScreen.gameObject.FindGameObjectInChildren("Controls");
        if (!achievementsMenuControls) {
            Logger.Warn("achievementsMenuControls is null");
            return;
        }

        var achievementsBackBtn = achievementsMenuControls.FindGameObjectInChildren("BackButton");
        if (!achievementsBackBtn) {
            Logger.Warn("achievementsBackBtn is null");
            return;
        }

        _backButtonObj = Object.Instantiate(achievementsBackBtn, UiGameObject.transform);
        _backButtonObj.SetActive(false);
        
        var eventTrigger = _backButtonObj.GetComponent<EventTrigger>();
        eventTrigger.triggers.Clear();

        var menuButton = _backButtonObj.GetComponent<MenuButton>();
        menuButton.cancelAction = CancelAction.DoNothing;
        
        ChangeBtnTriggers(eventTrigger, () => UIManager.instance.StartCoroutine(ReturnToMainMenu()));

        _connectInterface.StartHostButtonPressed += (username, port) => {
            OpenSaveSlotSelection(saveSelected => {
                if (!saveSelected) {
                    return;
                }

                RequestServerStartHostEvent?.Invoke(port);
                RequestClientConnectEvent?.Invoke(LocalhostAddress, port, username, true);
            });
        };

        _connectInterface.ConnectButtonPressed += (address, port, username) => {
            RequestClientConnectEvent?.Invoke(address, port, username, false);
        };

        On.UIManager.ReturnToMainMenu += (orig, self) => {
            RequestClientDisconnectEvent?.Invoke();
            RequestServerStopHostEvent?.Invoke();
            
            return orig(self);
        };

        // Hook to make sure that after game completion cutscenes we do not head to the main menu, but stay hosting/
        // connected to the server. Otherwise, if the host would go to the main menu, every other player would be
        // disconnected
        On.CutsceneHelper.DoSceneLoad += (orig, self) => {
            var sceneName = self.gameObject.scene.name;
            
            Logger.Debug($"DoSceneLoad of CutsceneHelper for next scene type: {self.nextSceneType}, scene name: {sceneName}");

            var toMainMenu = self.nextSceneType.Equals(CutsceneHelper.NextScene.MainMenu) 
                             || self.nextSceneType.Equals(CutsceneHelper.NextScene.MainMenuNoSave);
            if (self.nextSceneType.Equals(CutsceneHelper.NextScene.PermaDeathUnlock)) {
                toMainMenu |= GM.GetStatusRecordInt("RecPermadeathMode") != 0;
            }

            if (toMainMenu) {
                if (PlayerData.instance.GetInt("permadeathMode") != 0) {
                    // We are running Steel Soul mode, so we disconnect and go to main menu instead of reloading to
                    // the last save point
                    Logger.Debug("  NextSceneType is main menu, disconnecting because of Steel Soul");
                    
                    RequestClientDisconnectEvent?.Invoke();
                    RequestServerStopHostEvent?.Invoke();
                    
                    orig(self);
                    return;
                }
                
                Logger.Debug("  NextSceneType is main menu, transitioning to last save point instead");
                
                GameManager.instance.ContinueGame();
                return;
            }

            orig(self);
        };
    }
    
    /// <summary>
    /// Enter the game with the current PlayerData from the multiplayer menu. This assumes that the PlayerData
    /// instance is populated with values already.
    /// </summary>
    public void EnterGameFromMultiplayerMenu(bool newGame) {
        IH.StopUIInput();

        _pauseMenuGroup.SetActive(false);
        _backButtonObj.SetActive(false);

        UM.uiAudioPlayer.PlayStartGame();
        if (MenuStyles.Instance) {
            MenuStyles.Instance.StopAudio();
        }

        if (newGame) {
            Logger.Debug("Entering game from MP menu for new game");
            GM.StartCoroutine(GM.RunStartNewGame());
        } else {
            Logger.Debug("Entering game from MP menu for a continued game");
            GM.ContinueGame();
        }
    }
    
    /// <summary>
    /// Return to the main menu from in-game. Used whenever the player disconnects from the current server.
    /// </summary>
    public void ReturnToMainMenuFromGame() {
        IH.StopUIInput();
        
        UM.StartCoroutine(GM.ReturnToMainMenu(
            GameManager.ReturnToMainMenuSaveModes.DontSave,
            _ => {
                IH.StartUIInput();
                UM.returnMainMenuPrompt.HighlightDefault();
            }
        ));
    }

    /// <summary>
    /// Open the save slot selection screen (from the multiplayer connect menu) and execute the given callback when
    /// a save is selected.
    /// </summary>
    /// <param name="callback">The action to execute when save slot selection is finished. The boolean parameter
    /// indicates whether a save slot is selected (true) or the back button was pressed (false). Can be null, in which
    /// case no callback is executed.</param>
    public void OpenSaveSlotSelection(Action<bool> callback = null) {
        _saveSlotSelectedAction = SaveSlotSelectedCallback;

        On.GameManager.StartNewGame += OnStartNewGame;
        On.GameManager.ContinueGame += OnContinueGame;

        UM.StartCoroutine(GoToSaveMenu());
        return;

        void SaveSlotSelectedCallback(bool saveSelected) {
            callback?.Invoke(saveSelected);

            On.GameManager.StartNewGame -= OnStartNewGame;
            On.GameManager.ContinueGame -= OnContinueGame;
        }
    }
    
    /// <summary>
    /// Callback method for when a new game is started. This is used to check when to start a hosted server from
    /// the save menu.
    /// </summary>
    private void OnStartNewGame(On.GameManager.orig_StartNewGame orig, GameManager self, bool permaDeathMode, bool bossRushMode) {
        orig(self, permaDeathMode, bossRushMode);
        _saveSlotSelectedAction.Invoke(true);
    }

    /// <summary>
    /// Callback method for when a save file is continued. This is used to check when to start a hosted server from
    /// the save menu.
    /// </summary>
    private void OnContinueGame(On.GameManager.orig_ContinueGame orig, GameManager self) {
        orig(self);
        _saveSlotSelectedAction.Invoke(true);
    }

    /// <summary>
    /// Try to add the multiplayer option to the menu screen. Will not add the option if is already exists.
    /// </summary>
    private void TryAddMultiOption() {
        Logger.Info("AddMultiOption called");

        var btnParent = UM.mainMenuButtons.gameObject;
        if (!btnParent) {
            Logger.Info("btnParent is null");
            return;
        }

        var startMultiBtn = btnParent.FindGameObjectInChildren("StartMultiplayerButton");
        if (startMultiBtn) {
            Logger.Info("Multiplayer button is already present");
            
            FixMultiplayerButtonNavigation(startMultiBtn);
            
            return;
        }

        var startGameBtn = UM.mainMenuButtons.startButton.gameObject;
        if (!startGameBtn) {
            Logger.Info("startGameBtn is null");
            return;
        }

        startMultiBtn = Object.Instantiate(startGameBtn, btnParent.transform);
        if (!startMultiBtn) {
            Logger.Info("startMultiBtn is null");
            return;
        }

        startMultiBtn.name = "StartMultiplayerButton";
        startMultiBtn.transform.SetSiblingIndex(1);

        var autoLocalize = startMultiBtn.GetComponent<AutoLocalizeTextUI>();
        autoLocalize.textKey = "StartMultiplayerBtn";
        autoLocalize.RefreshTextFromLocalization();

        var eventTrigger = startMultiBtn.GetComponent<EventTrigger>();
        eventTrigger.triggers.Clear();
        
        ChangeBtnTriggers(eventTrigger, () => UM.StartCoroutine(GoToMultiplayerMenu()));
        
        // Fix navigation now and when the IH can accept input again
        FixMultiplayerButtonNavigation(startMultiBtn);
        
        UM.StartCoroutine(WaitForInput());
        IEnumerator WaitForInput() {
            yield return new WaitUntil(() => IH.acceptingInput);
            
            FixMultiplayerButtonNavigation(startMultiBtn);   
        }
    }
    
    /// <summary>
    /// Fix the navigation for the multiplayer button that is added to the main menu.
    /// </summary>
    /// <param name="multiBtnObject">The game object for the multiplayer button.</param>
    private void FixMultiplayerButtonNavigation(GameObject multiBtnObject) {
        // Fix navigation for buttons
        var startMultiBtnMenuBtn = multiBtnObject.GetComponent<MenuButton>();
        if (startMultiBtnMenuBtn) {
            var nav = UM.mainMenuButtons.startButton.navigation;
            nav.selectOnDown = startMultiBtnMenuBtn;
            UM.mainMenuButtons.startButton.navigation = nav;

            nav = UM.mainMenuButtons.optionsButton.navigation;
            nav.selectOnUp = startMultiBtnMenuBtn;
            UM.mainMenuButtons.optionsButton.navigation = nav;

            nav = startMultiBtnMenuBtn.navigation;
            nav.selectOnUp = UM.mainMenuButtons.startButton;
            startMultiBtnMenuBtn.navigation = nav;
        }
    }

    /// <summary>
    /// Coroutine to go to the multiplayer menu of the main menu. 
    /// </summary>
    private IEnumerator GoToMultiplayerMenu() {
        IH.StopUIInput();

        if (UM.menuState == MainMenuState.MAIN_MENU) {
            UM.StartCoroutine(ReflectionHelper.CallMethod<UIManager, IEnumerator>(UM, "FadeOutSprite", UM.gameTitle));
            UM.subtitleFSM.SendEvent("FADE OUT");
            yield return UM.StartCoroutine(UM.FadeOutCanvasGroup(UM.mainMenuScreen));
        } else if (UM.menuState == MainMenuState.SAVE_PROFILES) {
            yield return UM.StartCoroutine(UM.HideSaveProfileMenu());
        }
        
        IH.StartUIInput();
        
        _pauseMenuGroup.SetActive(true);
        _backButtonObj.SetActive(true);
    }

    /// <summary>
    /// Coroutine to go back to the main menu from the multiplayer menu.
    /// </summary>
    /// <returns></returns>
    private IEnumerator ReturnToMainMenu() {
        IH.StopUIInput();
        
        _pauseMenuGroup.SetActive(false);
        _backButtonObj.SetActive(false);
        
        UM.gameTitle.gameObject.SetActive(true);
        UM.mainMenuScreen.gameObject.SetActive(true);

        if (MenuStyles.Instance) {
            MenuStyles.Instance.UpdateTitle();
        }

        UM.StartCoroutine(ReflectionHelper.CallMethod<UIManager, IEnumerator>(UM, "FadeInSprite", UM.gameTitle));
        UM.subtitleFSM.SendEvent("FADE IN");

        yield return UM.StartCoroutine(UM.FadeInCanvasGroup(UM.mainMenuScreen));

        UM.mainMenuScreen.interactable = true;
        
        IH.StartUIInput();

        yield return null;
        
        UM.mainMenuButtons.HighlightDefault();

        UM.menuState = MainMenuState.MAIN_MENU;
    }

    /// <summary>
    /// Coroutine to go to the saves menu from the multiplayer menu. Used whenever the user selects to host a server.
    /// </summary>
    private IEnumerator GoToSaveMenu() {
        _pauseMenuGroup.SetActive(false);
        _backButtonObj.SetActive(false);
        
        yield return UM.GoToProfileMenu();
        
        var saveProfilesBackBtn = UM.saveProfileControls.gameObject.FindGameObjectInChildren("BackButton");
        if (!saveProfilesBackBtn) {
            Logger.Info("saveProfilesBackBtn is null");
            yield break;
        }

        var eventTrigger = saveProfilesBackBtn.GetComponent<EventTrigger>();
        _originalBackTriggers = eventTrigger.triggers;

        eventTrigger.triggers = [];
        ChangeBtnTriggers(eventTrigger, () => {
            On.GameManager.StartNewGame -= OnStartNewGame;
            On.GameManager.ContinueGame -= OnContinueGame;

            _saveSlotSelectedAction.Invoke(false);
            
            UM.StartCoroutine(GoToMultiplayerMenu());
            
            eventTrigger.triggers = _originalBackTriggers;
        });
    }

    /// <summary>
    /// Change the triggers on a button with the given event trigger.
    /// </summary>
    /// <param name="eventTrigger">The event trigger of the button to change.</param>
    /// <param name="action">The action that should be executed whenever the button is triggered.</param>
    private void ChangeBtnTriggers(EventTrigger eventTrigger, Action action) {
        var entry = new EventTrigger.Entry {
            eventID = EventTriggerType.Submit
        };
        entry.callback.AddListener(_ => action.Invoke());
        eventTrigger.triggers.Add(entry);
        
        var entry2 = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerClick
        };
        entry2.callback.AddListener(_ => action.Invoke());
        eventTrigger.triggers.Add(entry2);
    }
    
    #region Internal UI manager methods

    /// <summary>
    /// Callback method for when the client successfully connects.
    /// </summary>
    public void OnSuccessfulConnect() {
        _connectInterface.OnSuccessfulConnect();
        _pingInterface.SetEnabled(true);
    }

    /// <summary>
    /// Callback method for when the client fails to connect.
    /// </summary>
    /// <param name="result">The result of the failed connection.</param>
    public void OnFailedConnect(ConnectionFailedResult result) {
        _connectInterface.OnFailedConnect(result);
    }

    /// <summary>
    /// Callback method for when the client disconnects.
    /// </summary>
    public void OnClientDisconnect() {
        _connectInterface.OnClientDisconnect();
        _pingInterface.SetEnabled(false);
    }

    #endregion

    #region IUiManager methods

    /// <inheritdoc />
    public void DisableTeamSelection() {
    }

    /// <inheritdoc />
    public void EnableTeamSelection() {
    }

    /// <inheritdoc />
    public void DisableSkinSelection() {
        // SettingsInterface.OnAddonSetSkinSelection(false);
    }

    /// <inheritdoc />
    public void EnableSkinSelection() {
        // SettingsInterface.OnAddonSetSkinSelection(true);
    }

    #endregion
}
