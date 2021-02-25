using HKMP.Game;
using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.Util;
using ModCommon.Util;
using UnityEngine.SceneManagement;

namespace HKMP.Animation {
    /**
     * Class that manages all forms of animation from clients.
     */
    public class AnimationManager {
        private readonly NetworkManager _networkManager;
        private readonly PlayerManager _playerManager;

        private string _lastAnimationClip;

        public AnimationManager(NetworkManager networkManager, PlayerManager playerManager,
            PacketManager packetManager) {
            _networkManager = networkManager;
            _playerManager = playerManager;

            // Register packet handlers
            packetManager.RegisterClientPacketHandler(PacketId.PlayerAnimationUpdate, OnPlayerAnimationUpdate);

            // Register scene change, which is where we update the animation event handler
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
        }

        private void OnPlayerAnimationUpdate(Packet packet) {
            // Read ID and clip name from packet
            var id = packet.ReadInt();
            var clipName = packet.ReadString();

            UpdateAnimation(id, clipName);
        }

        public void UpdateAnimation(int id, string clipName) {
            var playerObject = _playerManager.GetPlayerObject(id);
            if (playerObject == null) {
                Logger.Warn(this,
                    $"Tried to update animation, but there was not matching player object for ID {id}");
                return;
            }
            
            var spriteAnimator = playerObject.GetComponent<tk2dSpriteAnimator>();
            spriteAnimator.Stop();
            spriteAnimator.Play(clipName);

            // TODO: extend this with complex animation that require more gameObjects
        }

        private void OnSceneChange(Scene oldScene, Scene newScene) {
            // Only update animation handler if we change from non-gameplay to a gameplay scene
            if (SceneUtil.IsNonGameplayScene(oldScene.name) && !SceneUtil.IsNonGameplayScene(newScene.name)) {
                // Obtain sprite animator from hero controller
                var localPlayer = HeroController.instance;
                var spriteAnimator = localPlayer.GetComponent<tk2dSpriteAnimator>();

                // For each clip in the animator, we want to make sure it triggers an event
                foreach (var clip in spriteAnimator.Library.clips) {
                    // Skip clips with no frames
                    if (clip.frames.Length == 0) {
                        continue;
                    }

                    var firstFrame = clip.frames[0];
                    // Enable event triggering on first frame
                    firstFrame.triggerEvent = true;
                    // Also include the clip name as event info, so we can retrieve it later
                    firstFrame.eventInfo = clip.name;
                }

                // Now actually register a callback for when the animation event fires
                spriteAnimator.AnimationEventTriggered = OnAnimationEvent;

                // Locate the spell control FSM
                var spellControlFSM = localPlayer.gameObject.LocateMyFSM("Spell Control");
                var actionLength = spellControlFSM.GetState("Q2 Pillar").Actions.Length;
                // Q2 Land state resets the animators event triggered callbacks, so we re-register it when that happens
                spellControlFSM.InsertMethod("Q2 Pillar", actionLength, () => {
                    spriteAnimator.AnimationEventTriggered = OnAnimationEvent;
                });
            }
        }

        private void OnAnimationEvent(tk2dSpriteAnimator spriteAnimator, tk2dSpriteAnimationClip clip,
            int frameIndex) {
            // Logger.Info(this, $"AnimationEvent clip name: {clip.name}");
            
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }

            // Skip event handling when we already handled this clip, unless it is a clip with wrap mode once
            if (clip.name.Equals(_lastAnimationClip) && clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Once) {
                return;
            }

            // Skip clips that do not have the wrap mode loop, loopsection or once
            if (clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Loop &&
                clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.LoopSection &&
                clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Once) {
                return;
            }

            // Get the current frame and associated data
            // TODO: the eventInfo might be same as the clip name in all cases
            var frame = clip.GetFrame(frameIndex);
            var clipName = frame.eventInfo;

            // Prepare an animation packet to be send
            var animationUpdatePacket = new Packet(PacketId.PlayerAnimationUpdate);
            animationUpdatePacket.Write(clipName);

            _networkManager.GetNetClient().SendUdp(animationUpdatePacket);

            // Update the last clip name, since it changed
            _lastAnimationClip = clip.name;
        }
    }
}