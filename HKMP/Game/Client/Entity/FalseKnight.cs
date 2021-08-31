using System.Collections;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Game.Client.Entity {
    public class FalseKnight : Entity {
        private readonly PlayMakerFSM _fsm;

        private Animation _lastAnimation;

        private bool _isDeathAnimationPlaying;

        private Coroutine _lastCoroutine;
        
        public FalseKnight(
            NetClient netClient, 
            byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.FalseKnight, entityId, gameObject) {
            _fsm = gameObject.LocateMyFSM("FalseyControl");

            CreateAnimationEvents();
        }

        private void CreateAnimationEvents() {
            _fsm.InsertMethod("Start Fall", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Fall);
                
                SendStateUpdate((byte) State.Default);
            }));
            
            _fsm.InsertMethod("Rubble End", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Land, 0);
            }));
            
            _fsm.InsertMethod("Jump Antic", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Jump);
            }));
            
            _fsm.InsertMethod("Land Noise", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Land, 1);
            }));

            _fsm.InsertMethod("Idle", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Idle);
            }));
            
            _fsm.InsertMethod("Run Antic", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Run);
            }));
            
            _fsm.InsertMethod("JA Antic", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.JumpAttack);
            }));
            
            _fsm.InsertMethod("JA Hit", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.JumpAttackHit);
            }));
            
            _fsm.InsertMethod("JA Slam", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.JumpAttackSlam);
            }));
            
            // This event is inserted at index 2, since after it we know that it will play the Jump animation
            _fsm.InsertMethod("S Antic", 2, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.SmashJumpAntic);
            }));
            
            _fsm.InsertMethod("S Jump", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.SmashJump);
            }));
            
            _fsm.InsertMethod("S Land", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.SmashLand);
            }));
            
            _fsm.InsertMethod("Voice? 2", 0, CreateUpdateMethod(() => {
                var playAudio = _fsm.FsmVariables.GetFsmInt("Stunned Amount").Value != 0;
                var shockwaveGoingRight = _fsm.FsmVariables.GetFsmBool("Shockwave Going Right").Value;

                SendAnimationUpdate(
                    (byte) Animation.SmashAttack,
                    new[] {
                        (byte) (playAudio ? 1 : 0),
                        (byte) (shockwaveGoingRight ? 1 : 0)
                    }
                );
            }));
            
            _fsm.InsertMethod("Stun Start", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.StunRoll);
                
                SendStateUpdate((byte) State.Stunned);
            }));
            
            _fsm.InsertMethod("Stun Land", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.StunLand);
            }));
            
            _fsm.InsertMethod("Open Uuup", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.StunOpen);
                
                SendStateUpdate((byte) State.StunnedOpen);
            }));
            
            _fsm.InsertMethod("Opened", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.StunOpened);
            }));
            
            _fsm.InsertMethod("Hit", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.StunHit);
            }));
            
            _fsm.InsertMethod("Stun Fail", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.StunFail);
                
                SendStateUpdate((byte) State.Default);
            }));
            
            _fsm.InsertMethod("Recover", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.StunRecover);
                
                SendStateUpdate((byte) State.Default);
            }));
            
            _fsm.InsertMethod("Idle Pause", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.StunIdle);
            }));
            
            _fsm.InsertMethod("Rage Jump Antic", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.RageJump);
            }));
            
            _fsm.InsertMethod("State 2", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.RageLand);
            }));
            
            // Insert this at index 2 since that is passed the check and ensures the Rage animation is played
            _fsm.InsertMethod("Rage", 2, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.RageAttack);
            }));
            
            _fsm.InsertMethod("Rage Slam", 0, CreateUpdateMethod(() => {
                var shouldFloorCrack = 
                    _fsm.FsmVariables.GetFsmInt("Stunned Amount").Value == 2 &&
                    _fsm.FsmVariables.GetFsmInt("Rages").Value == 1;
                
                SendAnimationUpdate((byte) Animation.RageSlam, (byte) (shouldFloorCrack ? 1 : 0));
            }));
            
            _fsm.InsertMethod("Rage End", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.RageEnd);
            }));
            
            _fsm.InsertMethod("JA Antic 2", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.DeathJump);
            }));
            
            _fsm.InsertMethod("JA Hit 2", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.DeathJumpAttack);
            }));
            
            _fsm.InsertMethod("Floor Break", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.DeathFloorBreak);
            }));
            
            _fsm.InsertMethod("Death Land", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.DeathLand);
            }));
            
            _fsm.InsertMethod("Death Open", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.DeathOpen);
                
                SendStateUpdate((byte) State.Stunned);
            }));
            
            _fsm.InsertMethod("Opened 2", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.DeathStunOpened);
                
                SendStateUpdate((byte) State.StunnedOpen);
            }));
            
            _fsm.InsertMethod("Hit 2", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.DeathStunHit);
            }));
            
            _fsm.InsertMethod("Death Anim Start", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.DeathAnimationStart);
                
                SendStateUpdate((byte) State.DeathAnimationStart);
            }));
            
            _fsm.InsertMethod("Ready", 0, CreateUpdateMethod(() => {
                SendStateUpdate((byte) State.DeathAnimationReady);
            }));
        }

        protected override void InternalInitializeAsSceneHost() {
            var activeStateName = _fsm.ActiveStateName;

            switch (activeStateName) {
                case "Init":
                case "Dormant":
                    SendStateUpdate((byte) State.NotSpawned);
                    break;
                case "Check Direction":
                case "Stun Turn L":
                case "Stun Turn R":
                case "Stun Start":
                case "Stun In Air":
                case "Stun Land":
                case "Roll End":
                case "Pause Short":
                case "Pause Long":
                case "Check If GG":
                case "Death Open":
                case "Head Frame":
                    SendStateUpdate((byte) State.Stunned);
                    break;
                case "Open Uuup":
                case "Head Reset":
                case "Opened":
                case "Hit":
                case "Opened 2":
                case "Hit 2":
                    SendStateUpdate((byte) State.StunnedOpen);
                    break;
                case "Death Anim Start":
                case "Open Map Shop and Journal":
                case "Steam":
                    SendStateUpdate((byte) State.DeathAnimationStart);
                    break;
                case "Ready":
                case "Beta End Event":
                case "Boss Death String":
                case "Set Head Facing":
                case "Blow":
                case "Death Head Land":
                case "Decrement Battle Enemies":
                case "Cough":
                    SendStateUpdate((byte) State.DeathAnimationReady);
                    break;
                default:
                    SendStateUpdate((byte) State.Default);
                    break;
            }
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_fsm);

            if (stateIndex.HasValue) {
                var state = (State) stateIndex.Value;
                
                Logger.Get().Info(this, $"Initializing with state: {state}");

                if (state == State.Default) {
                    _fsm.ExecuteActions("Start Fall", 1, 7);
                }

                if (state == State.Stunned) {
                    _fsm.ExecuteActions("Start Fall", 1);
                
                    _fsm.ExecuteActions("Stun Start", 3, 4);
                    
                    _fsm.ExecuteActions("Roll End", 4);
                }

                if (state == State.StunnedOpen) {
                    _fsm.ExecuteActions("Start Fall", 1);
                    
                    _fsm.ExecuteActions("Stun Start", 3, 4);

                    _fsm.ExecuteActions("Opened", 1);
                }

                if (state == State.DeathAnimationStart) {
                    _fsm.ExecuteActions("Start Fall", 1);

                    _fsm.ExecuteActions("Head Frame", 0);
                    
                    _fsm.ExecuteActions("Opened 2", 1, 2, 3, 4);

                    _isDeathAnimationPlaying = true;

                    RestoreAllTransitions(_fsm);
                    
                    _fsm.SetState("Death Anim Start");

                    Destroy();
                }

                if (state == State.DeathAnimationReady) {
                    _fsm.ExecuteActions("Start Fall", 1);

                    _fsm.ExecuteActions("Head Frame", 0);
                    
                    _fsm.ExecuteActions("Opened 2", 1, 2, 3, 4);

                    _isDeathAnimationPlaying = true;
                    
                    RestoreAllTransitions(_fsm);

                    _fsm.SetState("Ready");

                    Destroy();
                }
            }
        }

        protected override void InternalSwitchToSceneHost() {
            if (_isDeathAnimationPlaying) {
                return;
            }
        
            RestoreAllTransitions(_fsm);

            switch (_lastAnimation) {
                case Animation.Fall:
                    _fsm.SetState("State 1");
                    break;
                case Animation.Jump:
                    _fsm.SetState("Fall");
                    break;
                case Animation.Land:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Idle:
                    _fsm.SetState("Idle");
                    break;
                case Animation.JumpAttack:
                    _fsm.SetState("JA Fall");
                    break;
                case Animation.JumpAttackHit:
                    _fsm.SetState("JA Hit");
                    break;
                case Animation.JumpAttackSlam:
                    _fsm.SetState("JA Recoil");
                    break;
                case Animation.SmashJumpAntic:
                    _fsm.SetState("S Antic");
                    break;
                case Animation.SmashJump:
                    _fsm.SetState("S Rise");
                    break;
                case Animation.SmashLand:
                    _fsm.SetState("Voice? 2");
                    break;
                case Animation.SmashAttack:
                    _fsm.SetState("Idle");
                    break;
                case Animation.StunRoll:
                    _fsm.SetState("Stun Start");
                    break;
                case Animation.StunLand:
                    _fsm.SetState("Roll End");
                    break;
                case Animation.StunOpen:
                case Animation.StunOpened:
                    _fsm.SetState("Opened");
                    break;
                case Animation.StunHit:
                    _fsm.SetState("Hit");
                    break;
                case Animation.StunFail:
                    _fsm.SetState("Idle");
                    break;
                case Animation.StunRecover:
                case Animation.StunIdle:
                    _fsm.SetState("Idle Pause");
                    break;
                case Animation.RageJump:
                    _fsm.SetState("Jump 2");
                    break;
                case Animation.RageLand:
                    _fsm.SetState("R Attack Antic");
                    break;
                case Animation.RageAttack:
                    _fsm.SetState("Rage");
                    break;
                case Animation.RageSlam:
                    _fsm.SetState("Floor Crack?");
                    break;
                case Animation.RageEnd:
                    _fsm.SetState("Idle");
                    break;
                case Animation.DeathJump:
                    _fsm.SetState("JA Rise 2");
                    break;
                case Animation.DeathJumpAttack:
                    _fsm.SetState("Floor Break");
                    break;
                case Animation.DeathFloorBreak:
                    _fsm.SetState("Floor Break");
                    break;
                case Animation.DeathLand:
                    _fsm.SetState("Death Open");
                    break;
                case Animation.DeathOpen:
                case Animation.DeathStunOpened:
                    _fsm.SetState("Opened 2");
                    break;
                case Animation.DeathStunHit:
                    _fsm.SetState("Hit 2");
                    break;
            }
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            if (_isDeathAnimationPlaying) {
                return;
            }
            
            var animation = (Animation) animationIndex;

            _lastAnimation = animation;
            
            Logger.Get().Info(this, $"Received animation: {animation}");

            if (animation == Animation.Fall) {
                _fsm.ExecuteActions("Start Fall", 1, 2, 3, 4, 5, 6, 7, 11, 12);
            }

            if (animation == Animation.Jump) {
                _fsm.ExecuteActions("Jump Antic", 1);
                
                _fsm.GetAction<Tk2dWatchAnimationEvents>("Jump Antic", 2).Execute(() => {
                    _fsm.ExecuteActions("Jump", 0, 1, 2, 4, 6, 7);
                });
            }

            if (animation == Animation.Land) {
                var initialLand = animationInfo[0] == 0;

                if (initialLand) {
                    var battleSceneObject = GameObject.Find("Battle Scene");
                    PlayMakerFSM battleControl = null;
                    if (battleSceneObject != null) {
                        battleControl = battleSceneObject.LocateMyFSM("Battle Control");
                        if (battleControl != null) {
                            var activeState = battleControl.ActiveStateName;
                            Logger.Get().Info(this, $"Battle Control before active state: {activeState}");
                        } else {
                            Logger.Get().Info(this, "Battle Control FSM is null");
                        }
                    } else {
                        Logger.Get().Info(this, "Battle Scene object is null");
                    }
                    
                    _fsm.ExecuteActions("Rubble End", 1, 2, 3, 4);

                    if (battleSceneObject != null && battleControl != null) {
                        var activeState = battleControl.ActiveStateName;
                        Logger.Get().Info(this, $"Battle Control after active state: {activeState}");
                    }
                } else {
                    _fsm.ExecuteActions("Land Noise", 1);
                }

                _fsm.ExecuteActions("State 1", 1, 2);
                _fsm.GetAction<Tk2dWatchAnimationEvents>("State 1", 3).Execute(() => {
                    if (!initialLand) {
                        return;
                    }
                    
                    _fsm.ExecuteActions("Music", 0, 1, 2, 3, 4, 5);
                    
                    if (BossSceneController.IsBossScene) {
                        return;
                    }

                    _fsm.ExecuteActions("Music", 7, 8);
                    
                    _fsm.ExecuteActions("First Idle", 1, 2, 3);
                });
                _fsm.ExecuteActions("State 1", 4);
            }

            if (animation == Animation.Idle) {
                _fsm.ExecuteActions("Idle", 3, 4);
            }

            if (animation == Animation.Run) {
                _fsm.ExecuteActions("Run Antic", 1);
                _fsm.GetAction<Tk2dWatchAnimationEvents>("Run Antic", 2).Execute(() => {
                    _fsm.ExecuteActions("Run", 0, 1, 2, 3, 4);

                    _fsm.ExecuteActions("Voice?", 1);
                    
                    _fsm.ExecuteActions("JA Check Hero Pos", 0, 2, 3);
                });
            }

            if (animation == Animation.JumpAttack) {
                _fsm.ExecuteActions("JA Antic", 4);
                _fsm.GetAction<Tk2dWatchAnimationEvents>("JA Antic", 5).Execute(() => {
                    _fsm.ExecuteActions("JA Jump", 0, 1, 3, 5, 6);
                });
            }

            if (animation == Animation.JumpAttackHit) {
                _fsm.ExecuteActions("JA Hit", 1, 2, 3, 4);
            }

            if (animation == Animation.JumpAttackSlam) {
                _fsm.ExecuteActions("JA Slam", 1, 2, 3, 4, 5);
            
                _fsm.GetAction<Tk2dWatchAnimationEvents>("JA Slam", 6).Execute(() => {
                    if (_fsm.FsmVariables.GetFsmInt("Stunned Amount").Value >= 2) {
                        _fsm.ExecuteActions("Barrels?", 1, 2);
                    }
                    
                    _fsm.ExecuteActions("JA Recoil", 0, 1, 3);
                    _fsm.GetAction<Tk2dWatchAnimationEvents>("JA Recoil", 4).Execute(() => {
                        _fsm.ExecuteActions("JA Recoil 2", 0);
                    });
                });
                _fsm.ExecuteActions("JA Slam", 8);
            }

            if (animation == Animation.SmashJumpAntic) {
                _fsm.ExecuteActions("S Antic", 5);
            }

            if (animation == Animation.SmashJump) {
                _fsm.ExecuteActions("S Jump", 1, 2, 3, 4, 6, 7);
            }

            if (animation == Animation.SmashLand) {
                _fsm.ExecuteActions("S Land", 1, 2, 3, 5);
            }

            if (animation == Animation.SmashAttack) {
                var playAudio = animationInfo[0] == 1;
                var shockwaveGoingRight = animationInfo[1] == 1;

                _fsm.FsmVariables.GetFsmBool("Shockwave Going Right").Value = shockwaveGoingRight;
                _fsm.FsmVariables.GetFsmFloat("Shockwave X Origin").Value = shockwaveGoingRight ? 5.5f : -5.5f;

                if (playAudio) {
                    _fsm.ExecuteActions("Voice? 2", 2);
                }

                _lastCoroutine = MonoBehaviourUtil.Instance.StartCoroutine(PlaySmashAttackAnimation());
            }

            if (animation == Animation.StunRoll) {
                if (_lastCoroutine != null) {
                    MonoBehaviourUtil.Instance.StopCoroutine(_lastCoroutine);
                }
            
                _fsm.ExecuteActions("Check Direction", 0, 1, 2, 3, 4);
                
                _fsm.ExecuteActions("Stun Start", 1, 2, 3, 4, 5, 6, 8, 9, 10, 12, 13);
            }

            if (animation == Animation.StunLand) {
                _lastCoroutine = MonoBehaviourUtil.Instance.StartCoroutine(PlayStunLandAnimation());
            }

            if (animation == Animation.StunOpen) {
                _fsm.ExecuteActions("Open Uuup", 1, 2, 3, 4);
                
                _fsm.GetAction<Tk2dWatchAnimationEvents>("Open Uuup", 5).Execute(() => {
                    _fsm.ExecuteActions("Head Reset", 0);
                });
            }

            if (animation == Animation.StunOpened) {
                _fsm.ExecuteActions("Opened", 1, 2, 4, 5);
            }

            if (animation == Animation.StunHit) {
                _fsm.ExecuteActions("Hit", 1, 2, 3, 4, 5);
            }

            if (animation == Animation.StunFail) {
                _fsm.ExecuteActions("Stun Fail", 1, 2, 3, 4, 5, 6, 7, 9, 10);
            }

            if (animation == Animation.StunRecover) {
                _fsm.ExecuteActions("Recover", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 19);
            }

            if (animation == Animation.StunIdle) {
                _fsm.ExecuteActions("Idle Pause", 1, 3);
            }

            if (animation == Animation.RageJump) {
                _fsm.ExecuteActions("Rage Jump Antic", 5);
                
                _fsm.GetAction<Tk2dWatchAnimationEvents>("Rage Jump Antic", 6).Execute(() => {
                    _fsm.ExecuteActions("Jump 2", 1, 2, 3, 4, 5, 6);
                });
            }

            if (animation == Animation.RageLand) {
                _fsm.ExecuteActions("State 2", 2, 3, 4, 5);
                
                _fsm.GetAction<Tk2dWatchAnimationEvents>("State 2", 6).Execute(() => {
                    _fsm.ExecuteActions("R Attack Antic", 0, 1, 2);

                    _lastCoroutine = MonoBehaviourUtil.Instance.StartCoroutine(PlayRageBeginAnimation());
                });
            }

            if (animation == Animation.RageAttack) {
                _fsm.ExecuteActions("Rage", 0, 3);
            }

            if (animation == Animation.RageSlam) {
                var shouldFloorCrack = animationInfo[0] == 1;
                
                _fsm.ExecuteActions("Rage Slam", 1, 2, 3, 4, 5);

                if (shouldFloorCrack) {
                    _fsm.ExecuteActions("Floor Crack", 0, 1);
                }

                _lastCoroutine = MonoBehaviourUtil.Instance.StartCoroutine(PlayRageSlamAnimation());
            }

            if (animation == Animation.RageEnd) {
                _fsm.ExecuteActions("Rage Check", 0, 1, 2);
                
                _fsm.ExecuteActions("Rage End", 1, 2, 3, 4);
            }

            if (animation == Animation.DeathJump) {
                _fsm.ExecuteActions("Rage Check", 0, 1, 2);
            
                _fsm.ExecuteActions("JA Antic 2", 1, 2, 3, 8);
                
                _fsm.GetAction<Tk2dWatchAnimationEvents>("JA Antic 2", 9).Execute(() => {
                    _fsm.ExecuteActions("JA Jump 2", 0, 1, 3, 5, 6);
                });
            }

            if (animation == Animation.DeathJumpAttack) {
                _fsm.ExecuteActions("JA Hit 2", 1, 2, 3, 4);
            }

            if (animation == Animation.DeathFloorBreak) {
                _fsm.ExecuteActions("Floor Break", 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 13, 14, 17, 18);
            }

            if (animation == Animation.DeathLand) {
                _fsm.ExecuteActions("Death Land", 1, 2, 3, 4, 5, 6, 7);
            }

            if (animation == Animation.DeathOpen) {
                _fsm.ExecuteActions("Death Open", 1, 2, 3, 4);
                
                _fsm.GetAction<Tk2dWatchAnimationEvents>("Death Open", 5).Execute(() => {
                    _fsm.ExecuteActions("Head Frame", 0);
                });
            }

            if (animation == Animation.DeathStunOpened) {
                _fsm.ExecuteActions("Opened 2", 1, 2, 3, 4);
            }

            if (animation == Animation.DeathStunHit) {
                _fsm.ExecuteActions("Hit 2", 1, 2, 3, 4);
            }

            if (animation == Animation.DeathAnimationStart) {
                RestoreAllTransitions(_fsm);

                _fsm.SetState("Death Anim Start");

                Destroy();
            }
        }

        public override void UpdateState(byte state) {
        }

        private IEnumerator PlaySmashAttackAnimation() {
            _fsm.GetAction<Tk2dPlayAnimation>("S Attack Antic", 0).Execute();

            yield return new WaitForSeconds(1.2f);

            _fsm.GetAction<AudioPlaySimple>("S Attack", 0).Execute();
            _fsm.GetAction<ActivateGameObject>("S Attack", 1).Execute();
            _fsm.GetAction<SetCollider>("S Attack", 2).Execute();
            _fsm.GetAction<Tk2dPlayAnimation>("S Attack", 3).Execute();
            _fsm.GetAction<Tk2dPlayAnimation>("S Attack", 4).Execute();

            yield return new WaitForSeconds(0.12f);
            
            _fsm.GetAction<ActivateGameObject>("Slam", 0).Execute();
            _fsm.GetAction<AudioPlaySimple>("Slam", 1).Execute();
            _fsm.GetAction<SpawnObjectFromGlobalPool>("Slam", 2).Execute();
            _fsm.GetAction<SendEventByName>("Slam", 3).Execute();
            _fsm.GetAction<PlayParticleEmitter>("Slam", 4).Execute();
            _fsm.GetAction<Tk2dWatchAnimationEvents>("Slam", 5).Execute(() => {
                _fsm.GetAction<RandomInt>("S Attack Recover", 0).Execute();
                _fsm.GetAction<SetFsmInt>("S Attack Recover", 1).Execute();
                _fsm.GetAction<SendEventByName>("S Attack Recover", 2).Execute();
                _fsm.GetAction<SetVector3XYZ>("S Attack Recover", 3).Execute();
                _fsm.GetAction<SpawnObjectFromGlobalPool>("S Attack Recover", 4).Execute();
                _fsm.GetAction<SetScale>("S Attack Recover", 5).Execute();
                _fsm.GetAction<SetFsmBool>("S Attack Recover", 6).Execute();
                _fsm.GetAction<SetFsmFloat>("S Attack Recover", 7).Execute();
                _fsm.GetAction<Tk2dPlayAnimation>("S Attack Recover", 8).Execute();
                _fsm.GetAction<Tk2dPlayAnimation>("S Attack Recover", 9).Execute();
                _fsm.GetAction<ActivateGameObject>("S Attack Recover", 10).Execute();
            });
        }

        private IEnumerator PlayStunLandAnimation() {
            _fsm.ExecuteActions("Stun Land", 1, 2, 3, 4);

            yield return new WaitForSeconds(0.5f);

            _fsm.ExecuteActions("Roll End", 0, 1, 2, 3, 4);
        }

        private IEnumerator PlayRageBeginAnimation() {
            yield return new WaitForSeconds(0.7f);
            
            _fsm.ExecuteActions("Rage Begin", 0, 1, 2, 3, 4, 5);
            
            // Basically delay the execution of these actions by a frame, just like the FSM does
            ThreadUtil.RunActionOnMainThread(() => {
                _fsm.ExecuteActions("Rage", 0, 3);
            });
        }

        private IEnumerator PlayRageSlamAnimation() {
            yield return new WaitForSeconds(0.1f);
            
            _fsm.ExecuteActions("Particle End", 0);
            
            _fsm.GetAction<Tk2dWatchAnimationEvents>("Anim End", 0).Execute(() => {
                _fsm.ExecuteActions("Turn", 0, 1);
            });
        }

        private enum State {
            NotSpawned = 0,
            Default,
            Stunned,
            StunnedOpen,
            DeathAnimationStart,
            DeathAnimationReady
        }

        private enum Animation {
            Fall = 0,
            Jump,
            Land,
            Idle,
            Run,
            JumpAttack,
            JumpAttackHit,
            JumpAttackSlam,
            SmashJumpAntic,
            SmashJump,
            SmashLand,
            SmashAttack,
            StunRoll,
            StunLand,
            StunOpen,
            StunOpened,
            StunHit,
            StunFail,
            StunRecover,
            StunIdle,
            RageJump,
            RageLand,
            RageAttack,
            RageSlam,
            RageEnd,
            DeathJump,
            DeathJumpAttack,
            DeathFloorBreak,
            DeathLand,
            DeathOpen,
            DeathStunOpened,
            DeathStunHit,
            DeathAnimationStart
        }
    }
}