using System;
using System.Collections;
using System.Reflection;
using GlobalEnums;
using HKMP.Networking.Client;
using Modding;
using UnityEngine;

namespace HKMP.Game.Client {
    /**
     * Handles pause related things to prevent player being invincible in pause menu while connected to a server
     */
    public class PauseManager {
        private readonly NetClient _netClient;

        public PauseManager(NetClient netClient) {
            _netClient = netClient;
        }

        /**
         * Registers the required hooks
         */
        public void RegisterHooks() {
            On.InputHandler.Update += InputHandlerOnUpdate;
            On.UIManager.TogglePauseGame += UIManagerOnTogglePauseGame;
            
            On.HeroController.Pause += HeroControllerOnPause;
            On.TransitionPoint.OnTriggerEnter2D += TransitionPointOnOnTriggerEnter2D;
            On.HeroController.DieFromHazard += HeroControllerOnDieFromHazard;

            ModHooks.Instance.BeforePlayerDeadHook += OnDeath;
        }

        private void UIManagerOnTogglePauseGame(On.UIManager.orig_TogglePauseGame orig, UIManager self) {
            if (!_netClient.IsConnected) {
                orig(self);
                return;
            }
        
            // First evaluate whether the original method would have started the coroutine:
            // GameManager#PauseGameToggleByMenu
            var setTimeScale = !ReflectionHelper.GetAttr<UIManager, bool>(self, "ignoreUnpause");

            // Now we execute the original method, which will potentially set the timescale to 0f
            orig(self);

            // If we evaluated that the coroutine was started, we can now reset the timescale back to 1 again
            if (setTimeScale) {
                SetGameManagerTimeScale(1f);
            }
        }

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
                !PlayerData.instance.GetBool("disablePause")) {
                var state = global::GameManager.instance.gameState;
                if (state == GameState.PLAYING || state == GameState.PAUSED) {
                    setTimeScale = true;
                }
            }
            
            // Now we execute the original method, which will potentially set the timescale to 0f
            orig(self);

            // If we evaluated that the coroutine was started, we can now reset the timescale back to 1 again
            if (setTimeScale) {
                SetGameManagerTimeScale(1f);
            }
        }

        /**
         * If we are paused while the player dies, the game enters a state where the cursor is
         * visible while not in the pause menu, but not being able to give any input apart from opening the pause menu
         */
        private void OnDeath() {
            ImmediateUnpauseIfPaused();
        }

        /**
         * If we have a hazard respawn while in the pause menu it softlocks the menu, so we unpause it first
         */
        private IEnumerator HeroControllerOnDieFromHazard(On.HeroController.orig_DieFromHazard orig,
            HeroController self, HazardType hazardtype, float angle) {
            ImmediateUnpauseIfPaused();

            return orig(self, hazardtype, angle);
        }

        /**
         * If we go through a transition while being paused, we can only let the transition
         * occur if we unpause first and then let the original method continue
         */
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

        /**
         * If we don't reset the input of the hero when pausing, we might continue sliding across the floor
         * due to the timescale not being set to 0
         */
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

        /**
         * Unpauses the game immediately if it was paused
         */
        private void ImmediateUnpauseIfPaused() {
            if (UIManager.instance != null) {
                if (UIManager.instance.uiState.Equals(UIState.PAUSED)) {
                    var gm = global::GameManager.instance;

                    ReflectionHelper.GetAttr<global::GameManager, GameCameras>(gm, "gameCams").ResumeCameraShake();
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
        
        /**
         * Calls the SetTimeScale method in GameManager via reflection
         */
        public static void SetGameManagerTimeScale(float timeScale) {
            typeof(global::GameManager).InvokeMember(
                "SetTimeScale",
                BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                Type.DefaultBinder,
                global::GameManager.instance,
                new object[] {timeScale}
            );
        }
    }
}