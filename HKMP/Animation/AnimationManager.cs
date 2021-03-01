using System;
using System.Collections;
using System.Collections.Generic;
using HKMP.Animation.Effects;
using HKMP.Game;
using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HKMP.Animation {
    /**
     * Class that manages all forms of animation from clients.
     */
    public class AnimationManager {
        // Initialize animation effects that are used for different keys
        private static readonly Focus Focus = new Focus();
        private static readonly FocusBurst FocusBurst = new FocusBurst();
        private static readonly FocusEnd FocusEnd = new FocusEnd();
        // A static mapping containing the animation effect for each clip name
        private static readonly Dictionary<string, IAnimationEffect> AnimationEffects =
            new Dictionary<string, IAnimationEffect> {
                {"SD Dash", new CrystalDash()},
                {"SD Air Brake", new CrystalDashAirCancel()},
                {"SD Hit Wall", new CrystalDashHitWall()},
                {"Slash", new Slash()},
                {"SlashAlt", new AltSlash()},
                {"DownSlash", new DownSlash()},
                {"UpSlash", new UpSlash()},
                {"Wall Slash", new WallSlash()},
                {"Fireball1 Cast", new VengefulSpirit()},
                {"Fireball2 Cast", new ShadeSoul()},
                {"Quake Antic", new DiveAntic()},
                {"Quake Fall", new DesolateDiveDown()},
                {"Quake Fall 2", new DescendingDarkDown()},
                {"Quake Land", new DesolateDiveLand()},
                {"Quake Land 2", new DescendingDarkLand()},
                {"Scream", new HowlingWraiths()},
                {"Scream 2", new AbyssShriek()},
                {"NA Cyclone", new CycloneSlash()},
                {"NA Cyclone End", new CycloneSlashEnd()},
                {"NA Big Slash", new GreatSlash()},
                {"NA Dash Slash", new DashSlash()},
                {"Recoil", new Effects.Recoil()},
                {"Focus", Focus},
                {"Focus Get", FocusBurst},
                {"Focus Get Once", FocusEnd},
                {"Focus End", FocusEnd},
                {"Slug Down", Focus},
                {"Slug Burst", FocusBurst},
                {"Slug Up", FocusEnd}
            };

        private readonly NetworkManager _networkManager;
        private readonly PlayerManager _playerManager;

        private string _lastAnimationClip;

        public AnimationManager(NetworkManager networkManager, PlayerManager playerManager,
            PacketManager packetManager) {
            _networkManager = networkManager;
            _playerManager = playerManager;

            // Register packet handlers
            packetManager.RegisterClientPacketHandler<ClientPlayerAnimationUpdatePacket>(PacketId.ClientPlayerAnimationUpdate, OnPlayerAnimationUpdate);
            packetManager.RegisterClientPacketHandler<ClientPlayerDeathPacket>(PacketId.ClientPlayerDeath, OnPlayerDeath);

            // Register scene change, which is where we update the animation event handler
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;
        }

        private void OnPlayerAnimationUpdate(ClientPlayerAnimationUpdatePacket packet) {
            // Read ID and clip name from packet
            var id = packet.Id;
            var clipName = packet.ClipName;

            UpdatePlayerAnimation(id, clipName);

            if (AnimationEffects.ContainsKey(clipName)) {
                AnimationEffects[clipName].Play(
                    _playerManager.GetPlayerObject(id),
                    packet
                );
            }
        }

        public void UpdatePlayerAnimation(int id, string clipName) {
            var playerObject = _playerManager.GetPlayerObject(id);
            if (playerObject == null) {
                Logger.Warn(this,
                    $"Tried to update animation, but there was not matching player object for ID {id}");
                return;
            }

            var spriteAnimator = playerObject.GetComponent<tk2dSpriteAnimator>();
            spriteAnimator.Stop();
            spriteAnimator.Play(clipName);
        }

        private void OnSceneChange(Scene oldScene, Scene newScene) {
            // Only update animation handler if we change from non-gameplay to a gameplay scene
            if (SceneUtil.IsNonGameplayScene(oldScene.name) && !SceneUtil.IsNonGameplayScene(newScene.name)) {
                // Register on death, to send a packet to the server so clients can start the animation
                HeroController.instance.OnDeath += OnDeath;
                
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
                var spellControlFsm = localPlayer.gameObject.LocateMyFSM("Spell Control");
                var actionLength = spellControlFsm.GetState("Q2 Pillar").Actions.Length;
                // Q2 Land state resets the animators event triggered callbacks, so we re-register it when that happens
                spellControlFsm.InsertMethod("Q2 Pillar", actionLength,
                    () => { spriteAnimator.AnimationEventTriggered = OnAnimationEvent; });
            }
        }

        private void OnAnimationEvent(tk2dSpriteAnimator spriteAnimator, tk2dSpriteAnimationClip clip,
            int frameIndex) {
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }

            // Skip event handling when we already handled this clip, unless it is a clip with wrap mode once
            // if (clip.name.Equals(_lastAnimationClip) && clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Once) {
            //     return;
            // }

            // Skip clips that do not have the wrap mode loop, loopsection or once
            if (clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Loop &&
                clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.LoopSection &&
                clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Once) {
                return;
            }

            // Logger.Info(this, $"Sending animation with name: {clip.name}");
            
            // TODO: perhaps fix some animation issues here
            // Whenever we enter a building, the OnScene callback is execute later than
            // the enter animation is played, so a sequence of Exit -> Enter -> Idle -> Run should not be transmitted
            // Only the Exit should be transmitted

            // Get the current frame and associated data
            // TODO: the eventInfo might be same as the clip name in all cases
            var frame = clip.GetFrame(frameIndex);
            var clipName = frame.eventInfo;

            // Prepare an animation packet to be send
            var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                AnimationClipName = clipName
            };

            // Check whether there is an effect that adds info to this packet
            if (AnimationEffects.ContainsKey(clipName)) {
                AnimationEffects[clipName].PreparePacket(animationUpdatePacket);
            }
            
            animationUpdatePacket.CreatePacket();

            _networkManager.GetNetClient().SendUdp(animationUpdatePacket);

            // Update the last clip name, since it changed
            _lastAnimationClip = clip.name;
        }

        private void OnPlayerDeath(ClientPlayerDeathPacket packet) {
            // And play the death animation for the ID in the packet
            MonoBehaviourUtil.Instance.StartCoroutine(PlayDeathAnimation(packet.Id));
        }

        private void OnDeath() {
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }
            
            // Let the server know that we have died            
            var deathPacket = new ClientPlayerDeathPacket();
            deathPacket.CreatePacket();
            _networkManager.GetNetClient().SendTcp(deathPacket);
        }

        private IEnumerator PlayDeathAnimation(int id) {
            Logger.Info(this, "Starting death animation");
            
            // Get the player object corresponding to this ID
            var playerObject = _playerManager.GetPlayerObject(id);

            // Get the sprite animator and start playing the Death animation
            var animator = playerObject.GetComponent<tk2dSpriteAnimator>();
            animator.Stop();
            animator.PlayFromFrame("Death", 0);
            
            // Obtain the duration for the animation
            var deathAnimationDuration = animator.GetClipByName("Death").Duration;
            
            // After half a second we want to throw out the nail (as defined in the FSM)
            yield return new WaitForSeconds(0.5f);

            // Calculate the duration remaining until the death animation is finished
            var remainingDuration = deathAnimationDuration - 0.5f;

            // Obtain the local player object, to copy actions from
            var localPlayerObject = HeroController.instance.gameObject;
            
            // Get the FSM for the Hero Death
            var heroDeathAnimFsm = localPlayerObject
                .FindGameObjectInChildren("Hero Death")
                .LocateMyFSM("Hero Death Anim");

            // Get the nail fling object from the Blow state
            var nailObject = heroDeathAnimFsm.GetAction<FlingObjectsFromGlobalPool>("Blow", 0);

            // Spawn it relative to the player
            var nailGameObject = nailObject.gameObject.Value.Spawn(
                playerObject.transform.position,
                Quaternion.Euler(Vector3.zero)
            );

            // Get the rigidbody component that we need to throw around
            var nailRigidBody = nailGameObject.GetComponent<Rigidbody2D>();

            // Get a random speed and angle and calculate the rigidbody velocity
            var speed = UnityEngine.Random.Range(18, 22);
            float angle = UnityEngine.Random.Range(50, 130);
            var velX = speed * Mathf.Cos(angle * ((float) Math.PI / 180f));
            var velY = speed * Mathf.Sin(angle * ((float) Math.PI / 180f));

            // Set the velocity so it starts moving
            nailRigidBody.velocity = new Vector2(velX, velY);

            // Wait for the remaining duration of the death animation
            yield return new WaitForSeconds(remainingDuration);
            
            // Now we can disable the player object so it isn't visible anymore
            playerObject.SetActive(false);

            // Check which direction we are facing, we need this in a few variables
            var facingRight = playerObject.transform.localScale.x > 0;
            
            // Depending on which direction the player was facing, choose a state
            var stateName = "Head Left";
            if (facingRight) {
                stateName = "Head Right";
            }

            // Obtain a head object from the either Head states and instantiate it
            var headObject = heroDeathAnimFsm.GetAction<CreateObject>(stateName, 0);
            var headGameObject = Object.Instantiate(
                headObject.gameObject.Value,
                playerObject.transform.position + new Vector3(facingRight ? 0.2f : -0.2f, -0.02f, -0.01f),
                Quaternion.identity
            );

            // Get the rigidbody component of the head object
            var headRigidBody = headGameObject.GetComponent<Rigidbody2D>();
            
            // Calculate the angle at which we are going to throw 
            var headAngle = 15f * Mathf.Cos((facingRight ? 100f : 80f) * ((float) Math.PI / 180f));

            // Now set the velocity as this angle
            headRigidBody.velocity = new Vector2(headAngle, headAngle);
            
            // Finally add required torque (according to the FSM)
            headRigidBody.AddTorque(facingRight ? 20f : -20f);
        }
    }
}