using System.Collections;
using System.Reflection;
using GlobalEnums;
using Hkmp.Networking.Client;
using Modding;
using UnityEngine;

namespace Hkmp.Game.Client;

/// <summary>
/// Handles pause related things to prevent player being invincible in pause menu while connected to a server.
/// </summary>
internal class PauseManager {
    /// <summary>
    /// The net client instance.
    /// </summary>
    private readonly NetClient _netClient;

    public PauseManager(NetClient netClient) {
        _netClient = netClient;
    }

    /// <summary>
    /// Registers the required method hooks.
    /// </summary>
    public void RegisterHooks() {
        On.InputHandler.Update += InputHandlerOnUpdate;
        On.UIManager.TogglePauseGame += UIManagerOnTogglePauseGame;

        On.HeroController.Pause += HeroControllerOnPause;
        On.TransitionPoint.OnTriggerEnter2D += TransitionPointOnOnTriggerEnter2D;
        On.HeroController.DieFromHazard += HeroControllerOnDieFromHazard;

        ModHooks.BeforePlayerDeadHook += OnDeath;
    }

    /// <summary>
    /// Callback method for the UIManager#TogglePauseGame method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The UIManager instance.</param>
    private void UIManagerOnTogglePauseGame(On.UIManager.orig_TogglePauseGame orig, UIManager self) {
        if (!_netClient.IsConnected) {
            orig(self);
            return;
        }

        // First evaluate whether the original method would have started the coroutine:
        // GameManager#PauseGameToggleByMenu
        var setTimeScale = !ReflectionHelper.GetField<UIManager, bool>(self, "ignoreUnpause");

        // Now we execute the original method, which will potentially set the timescale to 0f
        orig(self);

        // If we evaluated that the coroutine was started, we can now reset the timescale back to 1 again
        if (setTimeScale) {
            SetTimeScale(1f);
        }
    }

    /// <summary>
    /// Callback method for the InputHandler#Update method.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The InputHandler instance.</param>
    private void InputHandlerOnUpdate(On.InputHandler.orig_Update orig, InputHandler self) {
        if (!_netClient.IsConnected) {
            orig(self);
            return;
        }

        // First evaluate whether the original method would have started the coroutine:
        // GameManager#PauseGameToggleByMenu
        var setTimeScale = false;

        if (self.acceptingInput &&
            self.inputActions.pause.WasPressed &&
            self.pauseAllowed &&
            !PlayerData.instance.GetBool(nameof(PlayerData.disablePause))) {
            var state = global::GameManager.instance.gameState;
            if (state == GameState.PLAYING || state == GameState.PAUSED) {
                setTimeScale = true;
            }
        }

        // Now we execute the original method, which will potentially set the timescale to 0f
        orig(self);

        // If we evaluated that the coroutine was started, we can now reset the timescale back to 1 again
        if (setTimeScale) {
            SetTimeScale(1f);
        }
    }

    /// <summary>
    /// Callback method for when the player dies.
    /// If we are paused while the player dies, the game enters a state where the cursor is visible
    /// while not in the pause menu, but not being able to give any input apart from opening the pause menu.
    /// Therefore, we unpause immediately before dying to prevent this.
    /// </summary>
    private void OnDeath() {
        ImmediateUnpauseIfPaused();
    }

    /// <summary>
    /// Callback method for the HeroController#DieFromHazard method.
    /// If we have a hazard respawn while in the pause menu it soft-locks the menu, so we unpause it first.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The HeroController instance.</param>
    /// <param name="hazardType">The type of hazard the player dies from.</param>
    /// <param name="angle">The angle of entering the hazard.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    private IEnumerator HeroControllerOnDieFromHazard(On.HeroController.orig_DieFromHazard orig,
        HeroController self, HazardType hazardType, float angle) {
        ImmediateUnpauseIfPaused();

        return orig(self, hazardType, angle);
    }

    /// <summary>
    /// Callback method for the TransitionPoint#OnTriggerEnter2D method.
    /// If we go through a transition while being paused, we can only let the transition occur if we
    /// unpause first and then let the original method continue.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The TransitionPoint instance.</param>
    /// <param name="obj">The collider that enters the trigger.</param>
    private void TransitionPointOnOnTriggerEnter2D(
        On.TransitionPoint.orig_OnTriggerEnter2D orig,
        TransitionPoint self,
        Collider2D obj
    ) {
        // Skip this if the transition point is a door, since it isn't a enter-and-teleport transition,
        // but requires input to transition, so it can't happen in the pause menu
        if (!self.isADoor) {
            ImmediateUnpauseIfPaused();
        }

        // Execute original method
        orig(self, obj);
    }

    /// <summary>
    /// Callback method for the HeroController#OnPause method.
    /// If we don't reset the input of the hero when pausing, we might continue sliding across the floor
    /// due to the timescale not being set to 0.
    /// </summary>
    /// <param name="orig">The original method.</param>
    /// <param name="self">The HeroController instance.</param>
    private void HeroControllerOnPause(On.HeroController.orig_Pause orig, HeroController self) {
        if (!_netClient.IsConnected) {
            orig(self);
            return;
        }

        // We simply call the private ResetInput method to prevent the knight from continuing movement
        // while the game is paused
        typeof(HeroController).InvokeMember(
            "ResetInput",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
            null,
            HeroController.instance,
            null
        );
    }

    /// <summary>
    /// Unpauses the game immediately if it was paused.
    /// </summary>
    private static void ImmediateUnpauseIfPaused() {
        if (UIManager.instance != null) {
            if (UIManager.instance.uiState.Equals(UIState.PAUSED)) {
                var gm = global::GameManager.instance;

                ReflectionHelper.GetField<global::GameManager, GameCameras>(gm, "gameCams").ResumeCameraShake();
                gm.inputHandler.PreventPause();
                gm.actorSnapshotUnpaused.TransitionTo(0f);
                gm.isPaused = false;
                gm.ui.AudioGoToGameplay(0.2f);
                gm.ui.SetState(UIState.PLAYING);
                gm.SetState(GameState.PLAYING);
                if (HeroController.instance != null) {
                    HeroController.instance.UnPause();
                }

                MenuButtonList.ClearAllLastSelected();
                gm.inputHandler.AllowPause();
            }
        }
    }

    /// <summary>
    /// Sets the time scale similarly to the method GameManager#SetTimeScale.
    /// </summary>
    /// <param name="timeScale">The new time scale.</param>
    public static void SetTimeScale(float timeScale) {
        TimeController.GenericTimeScale = timeScale > 0.00999999977648258 ? timeScale : 0.0f;
    }
}
