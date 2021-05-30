using System.Collections;
using System.Diagnostics;
using System.Reflection;
using GlobalEnums;
using HKMP.Networking.Client;
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

        public void RegisterHooks() {
            On.GameManager.PauseGameToggle += GameManagerOnPauseGameToggle;
            On.GameManager.SetTimeScale_float += GameManagerOnSetTimeScale_float;
            On.HeroController.Pause += HeroControllerOnPause;
            On.TransitionPoint.OnTriggerEnter2D += TransitionPointOnOnTriggerEnter2D;
        }

        /**
         * If we go through a transition while being based, we can only let the transition
         * occur if we unpause first and then let the original method continue
         */
        private void TransitionPointOnOnTriggerEnter2D(
            On.TransitionPoint.orig_OnTriggerEnter2D orig,
            TransitionPoint self,
            Collider2D obj
        ) {
            // Unpause if paused
            if (UIManager.instance != null) {
                if (UIManager.instance.uiState.Equals(UIState.PAUSED)) {
                    UIManager.instance.TogglePauseGame();
                }
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
         * If the timescale changes due to the game going to the pause menu, we prevent it from doing so
         */
        private void GameManagerOnSetTimeScale_float(
            On.GameManager.orig_SetTimeScale_float orig, 
            global::GameManager self, 
            float scale
        ) {
            // Get the stack trace and check whether a specific frame contains a method that is responsible
            // for pausing the game while going to menu
            var stackTrace = new StackTrace();
                
            var frames = stackTrace.GetFrames();
            if (frames == null) {
                orig(self, scale);
                return;
            }
                
            var name = frames[2].GetMethod().Name;
            if (!name.Contains("PauseGameToggle")) {
                orig(self, scale);
                return;
            }
                
            if (!_netClient.IsConnected) {
                orig(self, scale);
            } else {
                // Always put the time scale to 1.0, thus never allowing the game to change speed
                // This is to prevent desyncs in multiplayer
                orig(self, 1.0f);
            }
        }

        /**
         * Ignore execution of the PauseGameToggle method if represents an unpause due to UI elements
         * overlapping the pause menu
         */
        private IEnumerator GameManagerOnPauseGameToggle(
            On.GameManager.orig_PauseGameToggle orig, 
            global::GameManager self
        ) {
            if (!_netClient.IsConnected) {
                yield return orig(self);
                yield break;
            }
                
            var stackTrace = new StackTrace();
                
            var frames = stackTrace.GetFrames();
            if (frames == null) {
                yield return orig(self);
                yield break;
            }
            
            var methodName = frames[1].GetMethod().Name;
            if (methodName.Equals("MoveNext")) {
                yield break;
            }
                
            yield return orig(self);
        }
        
    }
}