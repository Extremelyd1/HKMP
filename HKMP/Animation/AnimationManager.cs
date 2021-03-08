using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GlobalEnums;
using HKMP.Animation.Effects;
using HKMP.Game;
using HKMP.Networking;
using HKMP.Networking.Packet;
using HKMP.Networking.Packet.Custom;
using HKMP.Util;
using HutongGames.PlayMaker.Actions;
using ModCommon;
using ModCommon.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace HKMP.Animation {
    /**
     * Class that manages all forms of animation from clients.
     */
    public class AnimationManager {
        public const float EffectDistanceThreshold = 25f;
        
        // Animations that are allowed to loop, because they need to transmit the effect
        private static readonly string[] AllowedLoopAnimations = {"Focus Get", "Run"};

        private static readonly string[] AnimationControllerClipNames = {
            "Airborne"
        };

        // Initialize animation effects that are used for different keys
        public static readonly CrystalDashChargeCancel CrystalDashChargeCancel = new CrystalDashChargeCancel();
        
        private static readonly Focus Focus = new Focus();
        private static readonly FocusBurst FocusBurst = new FocusBurst();

        private static readonly FocusEnd FocusEnd = new FocusEnd();

        // TODO: add hazard respawn effect
        // A static mapping containing the animation effect for each clip name
        private static readonly Dictionary<string, IAnimationEffect> AnimationEffects =
            new Dictionary<string, IAnimationEffect> {
                {"SD Charge Ground", new CrystalDashGroundCharge()},
                {"SD Charge Ground End", CrystalDashChargeCancel},
                {"SD Wall Charge", new CrystalDashWallCharge()},
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
                {"Slug Up", FocusEnd},
                {"Dash", new Dash()},
                {"Dash Down", new DashDown()},
                {"Shadow Dash", new ShadowDash()},
                {"Shadow Dash Sharp", new ShadowDashSharp()},
                {"Shadow Dash Down", new ShadowDashDown()},
                {"Shadow Dash Down Sharp", new ShadowDashSharpDown()},
                {"Dash End", new DashEnd()},
                {"Nail Art Charge", new NailArtCharge()},
                {"Nail Art Charged", new NailArtCharged()},
                {"Nail Art Charge End", new NailArtEnd()},
                {"Wall Slide", new WallSlide()},
                {"Wall Slide End", new WallSlideEnd()},
                {"Walljump", new WallJump()},
                {"Double Jump", new MonarchWings()},
                {"HardLand", new HardLand()},
                {"Hazard Death", new HazardDeath()},
                {"Hazard Respawn", new HazardRespawn()}
            };

        private readonly NetworkManager _networkManager;
        private readonly PlayerManager _playerManager;

        // The last animation clip sent
        private string _lastAnimationClip;
        
        /**
         * Whether the animation controller was responsible for the last
         * clip that was sent
         */
        private bool _animationControllerWasLastSent;

        // Whether we should stop sending animations until the scene has changed
        private bool _stopSendingAnimationUntilSceneChange;

        // Whether the current dash has ended and we can start a new one
        private bool _dashHasEnded = true;
        
        // Whether the charge effect was last update active
        private bool _lastChargeEffectActive;
        // Whether the charged effect was last update active
        private bool _lastChargedEffectActive;
        
        // Whether the player was wallsliding last update
        private bool _lastWallSlideActive;

        public AnimationManager(
            NetworkManager networkManager,
            PlayerManager playerManager,
            PacketManager packetManager,
            Game.Settings.GameSettings gameSettings
        ) {
            _networkManager = networkManager;
            _playerManager = playerManager;

            // Register packet handlers
            packetManager.RegisterClientPacketHandler<ClientPlayerAnimationUpdatePacket>(
                PacketId.ClientPlayerAnimationUpdate, OnPlayerAnimationUpdate);
            packetManager.RegisterClientPacketHandler<ClientPlayerDeathPacket>(PacketId.ClientPlayerDeath,
                OnPlayerDeath);

            // Register scene change, which is where we update the animation event handler
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnSceneChange;

            // Register callbacks for the hero animation controller for the Airborne animation
            On.HeroAnimationController.Play += HeroAnimationControllerOnPlay;
            On.HeroAnimationController.PlayFromFrame += HeroAnimationControllerOnPlayFromFrame;

            // Register a callback so we know when the dash has finished
            On.HeroController.CancelDash += HeroControllerOnCancelDash;

            // Register a callback so we can check the nail art charge status
            ModHooks.Instance.HeroUpdateHook += OnHeroUpdateHook;

            // Register a callback for when we get hit by a hazard
            On.HeroController.DieFromHazard += HeroControllerOnDieFromHazard;
            // Also register a callback from when we respawn from a hazard
            On.GameManager.HazardRespawn += GameManagerOnHazardRespawn;

            // Set the game settings for all animation effects
            foreach (var effect in AnimationEffects.Values) {
                effect.SetGameSettings(gameSettings);
            }
        }

        private void OnPlayerAnimationUpdate(ClientPlayerAnimationUpdatePacket packet) {
            // Read ID and clip name from packet
            var id = packet.Id;
            var clipName = packet.ClipName;
            var frame = packet.Frame;

            UpdatePlayerAnimation(id, clipName, frame);

            if (AnimationEffects.ContainsKey(clipName)) {
                var playerObject = _playerManager.GetPlayerObject(id);
                if (playerObject == null) {
                    Logger.Warn(this,
                        $"Tried to play animation effect {clipName} with ID: {id}, but player object doesn't exist");
                    return;
                }
                
                AnimationEffects[clipName].Play(
                    playerObject,
                    packet
                );
            }
        }

        public void UpdatePlayerAnimation(int id, string clipName, int frame) {
            var playerObject = _playerManager.GetPlayerObject(id);
            if (playerObject == null) {
                Logger.Warn(this,
                    $"Tried to update animation, but there was not matching player object for ID {id}");
                return;
            }

            if (clipName.Length == 0) {
                Logger.Warn(this, "Tried to update animation with empty clip name");
                return;
            }

            // Get the sprite animator and check whether this clip can be played before playing it
            var spriteAnimator = playerObject.GetComponent<tk2dSpriteAnimator>();
            if (spriteAnimator.GetClipByName(clipName) != null) {
                spriteAnimator.PlayFromFrame(clipName, frame);
            }
        }

        private void OnSceneChange(Scene oldScene, Scene newScene) {
            // A scene change occurs, so we can send again
            _stopSendingAnimationUntilSceneChange = false;
            
            // Only update animation handler if we change from non-gameplay to a gameplay scene
            if (SceneUtil.IsNonGameplayScene(oldScene.name) && !SceneUtil.IsNonGameplayScene(newScene.name)) {
                // Register on death, to send a packet to the server so clients can start the animation
                HeroController.instance.OnDeath += OnDeath;
            }
        }

        private void OnAnimationEvent(tk2dSpriteAnimator spriteAnimator, tk2dSpriteAnimationClip clip,
            int frameIndex) {
            // Logger.Info(this, $"Animation event with name: {clip.name}");
            
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }

            // If we need to stop sending until a scene change occurs, we skip
            if (_stopSendingAnimationUntilSceneChange) {
                return;
            }
            
            // If this is a clip that should be handled by the animation controller hook, we return
            if (AnimationControllerClipNames.Contains(clip.name)) {
                // Update the last clip name
                _lastAnimationClip = clip.name;
                
                return;
            }

            // Skip event handling when we already handled this clip, unless it is a clip with wrap mode once
            if (clip.name.Equals(_lastAnimationClip) 
                && clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Once 
                && !AllowedLoopAnimations.Contains(clip.name)) {
                return;
            }

            // Skip clips that do not have the wrap mode loop, loopsection or once
            if (clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Loop &&
                clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.LoopSection &&
                clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Once) {
                return;
            }
            
            // Logger.Info(this, $"Sending animation with name: {clip.name}");

            // Make sure that when we enter a building, we don't transmit any more animation events
            // TODO: the same issue applied to exiting a building, but that is less trivial to solve
            if (clip.name.Equals("Enter")) {
                _stopSendingAnimationUntilSceneChange = true;
            }

            // Check special case of downwards dashes that trigger the animation event twice
            // We only send it once if the current dash has ended
            if (clip.name.Equals("Dash Down")
                || clip.name.Equals("Shadow Dash Down")
                || clip.name.Equals("Shadow Dash Down Sharp")) {
                if (!_dashHasEnded) {
                    return;
                }
                
                _dashHasEnded = false;
            }

            // Get the current frame and associated data
            // TODO: the eventInfo might be same as the clip name in all cases
            var frame = clip.GetFrame(frameIndex);
            var clipName = frame.eventInfo;

            // Prepare an animation packet to be send
            var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                AnimationClipName = clipName,
                Frame = 0
            };

            // Check whether there is an effect that adds info to this packet
            if (AnimationEffects.ContainsKey(clipName)) {
                AnimationEffects[clipName].PreparePacket(animationUpdatePacket);
            }

            animationUpdatePacket.CreatePacket();

            _networkManager.GetNetClient().SendUdp(animationUpdatePacket);

            // Update the last clip name, since it changed
            _lastAnimationClip = clip.name;

            // We have sent a different clip, so we can reset this
            _animationControllerWasLastSent = false;
        }

        private void HeroAnimationControllerOnPlay(On.HeroAnimationController.orig_Play orig, HeroAnimationController self, string clipname) {
            orig(self, clipname);
            OnAnimationControllerPlay(clipname, 0);
        }
        
        private void HeroAnimationControllerOnPlayFromFrame(On.HeroAnimationController.orig_PlayFromFrame orig, HeroAnimationController self, string clipname, int frame) {
            orig(self, clipname, frame);
            OnAnimationControllerPlay(clipname, frame);
        }

        private void OnAnimationControllerPlay(string clipName, int frame) {
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }
            
            // If this is not a clip that should be handled by the animation controller hook, we return
            if (!AnimationControllerClipNames.Contains(clipName)) {
                return;
            }

            // If the animation controller is responsible for the last sent clip, we skip
            // this is to ensure that we don't spam packets of the same clip
            if (!_animationControllerWasLastSent) {
                // Prepare an animation packet to be send
                var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                    AnimationClipName = clipName,
                    Frame = frame
                };
                animationUpdatePacket.CreatePacket();

                _networkManager.GetNetClient().SendUdp(animationUpdatePacket);
                
                // This was the last clip we sent
                _animationControllerWasLastSent = true;
            }
        }
        
        private void HeroControllerOnCancelDash(On.HeroController.orig_CancelDash orig, HeroController self) {
            orig(self);

            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }
            
            // Prepare an custom animation packet to be send
            var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                AnimationClipName = "Dash End",
                Frame = 0
            };
            animationUpdatePacket.CreatePacket();

            _networkManager.GetNetClient().SendUdp(animationUpdatePacket);

            // The dash has ended, so we can send a new one when we dash
            _dashHasEnded = true;
        }

        private void OnHeroUpdateHook() {
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }
            
            var chargeEffectActive = HeroController.instance.artChargeEffect.activeSelf;
            var chargedEffectActive = HeroController.instance.artChargedEffect.activeSelf;

            if (chargeEffectActive && !_lastChargeEffectActive) {
                // Charge effect is now active, which wasn't last update, so we can send the charge animation packet
                
                // Create an animation update packet with the Nail Art Charge name
                var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                    AnimationClipName = "Nail Art Charge",
                    Frame = 0
                };
                animationUpdatePacket.CreatePacket();
                _networkManager.GetNetClient().SendUdp(animationUpdatePacket);
            }

            if (chargedEffectActive && !_lastChargedEffectActive) {
                // Charged effect is now active, which wasn't last update, so we can send the charged animation packet
                
                // Create an animation update packet with the Nail Art Charge name
                var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                    AnimationClipName = "Nail Art Charged",
                    Frame = 0
                };
                animationUpdatePacket.CreatePacket();
                _networkManager.GetNetClient().SendUdp(animationUpdatePacket);
            }

            if (!chargeEffectActive && _lastChargeEffectActive && !chargedEffectActive) {
                // The charge effect is now inactive and we are not fully charged
                // This means that we cancelled the nail art charge
                
                // Create an animation update packet with the Nail Art Charge name
                var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                    AnimationClipName = "Nail Art Charge End",
                    Frame = 0
                };
                animationUpdatePacket.CreatePacket();
                _networkManager.GetNetClient().SendUdp(animationUpdatePacket);
            }

            if (!chargedEffectActive && _lastChargedEffectActive) {
                // The charged effect is now inactive, so we are done with the nail art
                
                // Create an animation update packet with the Nail Art Charge name
                var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                    AnimationClipName = "Nail Art Charge End",
                    Frame = 0
                };
                animationUpdatePacket.CreatePacket();
                _networkManager.GetNetClient().SendUdp(animationUpdatePacket);
            }

            // Update the latest states
            _lastChargeEffectActive = chargeEffectActive;
            _lastChargedEffectActive = chargedEffectActive;

            // Obtain the current wall slide state
            var wallSlideActive = HeroController.instance.cState.wallSliding;

            if (!wallSlideActive && _lastWallSlideActive) {
                // We were wall sliding last update, but not anymore, so we send a wall slide end animation
                var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                    AnimationClipName = "Wall Slide End",
                    Frame = 0
                };
                animationUpdatePacket.CreatePacket();
                _networkManager.GetNetClient().SendUdp(animationUpdatePacket);
            }
            
            // Update the last state
            _lastWallSlideActive = wallSlideActive;
            
            // Obtain sprite animator from hero controller
            var localPlayer = HeroController.instance;
            var spriteAnimator = localPlayer.GetComponent<tk2dSpriteAnimator>();

            // Check whether it is non-null
            if (spriteAnimator != null) {
                // Check whether the animation event is still registered to our callback
                if (spriteAnimator.AnimationEventTriggered != OnAnimationEvent) {
                    Logger.Info(this, "Re-registering animation event triggered");
                    
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
                }
            }
        }
        
        private IEnumerator HeroControllerOnDieFromHazard(On.HeroController.orig_DieFromHazard orig, HeroController self, HazardType hazardtype, float angle) {
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return orig(self, hazardtype, angle);
            }
            
            Logger.Info(this, $"DieFromHazard called: {hazardtype}, {angle}");
            
            // Create an animation update packet with the custom clip name
            var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                AnimationClipName = "Hazard Death",
                Frame = 0
            };
            
            // Add whether we died from spikes or from acid
            animationUpdatePacket.EffectInfo.Add(hazardtype.Equals(HazardType.SPIKES));
            animationUpdatePacket.EffectInfo.Add(hazardtype.Equals(HazardType.ACID));
            
            // Create and send the packet
            _networkManager.GetNetClient().SendUdp(animationUpdatePacket.CreatePacket());

            // Execute the original method and return its value
            return orig(self, hazardtype, angle);
        }
        
        private void GameManagerOnHazardRespawn(On.GameManager.orig_HazardRespawn orig, GameManager self) {
            orig(self);
            
            // If we are not connected, there is nothing to send to
            if (!_networkManager.GetNetClient().IsConnected) {
                return;
            }
            
            Logger.Info(this, "HazardRespawn called");
            
            // Create an animation update packet with the custom clip name
            var animationUpdatePacket = new ServerPlayerAnimationUpdatePacket {
                AnimationClipName = "Hazard Respawn",
                Frame = 0
            };
            
            // Create and send the packet
            _networkManager.GetNetClient().SendUdp(animationUpdatePacket.CreatePacket());
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

            Logger.Info(this, "Client has died, sending PlayerDeath packet");

            // Let the server know that we have died            
            var deathPacket = new ServerPlayerDeathPacket();
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