using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Game.Client.Entity {
    public class BattleGate : Entity {
        private readonly PlayMakerFSM _fsm;

        private State _lastState;
        
        public BattleGate(
            NetClient netClient, 
             byte entityId,
            GameObject gameObject
        ) : base(netClient, EntityType.BattleGate, entityId, gameObject) {
            _fsm = gameObject.LocateMyFSM("BG Control");

            CreateAnimationEvents();
        }

        private void CreateAnimationEvents() {
            _fsm.InsertMethod("Close 1", 0, CreateStateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Close);
                
                SendStateUpdate((byte) State.Closed);
            }));
            
            _fsm.InsertMethod("Open", 0, CreateStateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.Open);
                
                SendStateUpdate((byte) State.Open);
            }));
            
            _fsm.InsertMethod("Quick Close", 0, CreateStateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.QuickClose);

                SendStateUpdate((byte) State.Closed);
            }));
            
            _fsm.InsertMethod("Quick Open", 0, CreateStateUpdateMethod(() => {
                SendAnimationUpdate((byte) Animation.QuickOpen);
                
                SendStateUpdate((byte) State.Open);
            }));
        }

        protected override void InternalInitializeAsSceneHost() {
            if (_fsm.FsmVariables.GetFsmBool("Start Closed").Value) {
                SendStateUpdate((byte) State.Closed);
            } else {
                SendStateUpdate((byte) State.Open);
            }
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_fsm);
            
            _fsm.GetAction<GetOwner>("Opened", 0).Execute();
            
            if (stateIndex.HasValue) {
                var state = (State) stateIndex.Value;

                if (state == State.Closed) {
                    _fsm.GetAction<SetCollider>("Quick Close", 1).Execute();
                    _fsm.GetAction<Tk2dPlayAnimation>("Quick Close", 2).Execute();
                } else if (state == State.Open) {
                    _fsm.GetAction<Tk2dPlayAnimation>("Opened", 2).Execute();
                    _fsm.GetAction<SetCollider>("Opened", 3).Execute();
                }
            }
        }

        protected override void InternalSwitchToSceneHost() {
            RestoreAllTransitions(_fsm);

            switch (_lastState) {
                case State.Closed:
                    _fsm.SetState("Double Close");
                    break;
                case State.Open:
                    _fsm.SetState("Quick Open");
                    break;
            }
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            var animation = (Animation) animationIndex;
            
            Logger.Get().Info(this, $"Received Animation: {animation}");

            if (animation == Animation.Close) {
                _fsm.GetAction<AudioPlayerOneShotSingle>("Close 1", 1).Execute();
                _fsm.GetAction<Tk2dPlayAnimationWithEvents>("Close 1", 2).Execute(() => {
                    _fsm.GetAction<Tk2dPlayFrame>("Close 2", 0).Execute();
                    _fsm.GetAction<SetMeshRenderer>("Close 2", 1).Execute();
                    _fsm.GetAction<Tk2dPlayAnimation>("Close 2", 2).Execute();
                    _fsm.GetAction<SendEventByName>("Close 2", 3).Execute();
                    _fsm.GetAction<PlayParticleEmitter>("Close 2", 4).Execute();
                });
                _fsm.GetAction<SetCollider>("Close 1", 3).Execute();
            }

            if (animation == Animation.Open) {
                _fsm.GetAction<AudioPlayerOneShotSingle>("Open", 1).Execute();
                _fsm.GetAction<Tk2dPlayAnimation>("Open", 2).Execute();
                _fsm.GetAction<PlayParticleEmitter>("Open", 3).Execute();
                _fsm.GetAction<SetCollider>("Open", 4).Execute();
            }

            if (animation == Animation.QuickClose) {
                _fsm.GetAction<SetCollider>("Quick Close", 1).Execute();
                _fsm.GetAction<Tk2dPlayAnimation>("Quick Close", 2).Execute();
            }

            if (animation == Animation.QuickOpen) {
                _fsm.GetAction<Tk2dPlayAnimation>("Quick Open", 1).Execute();
                _fsm.GetAction<SetCollider>("Quick Open", 2).Execute();
            }
        }

        public override void UpdateState(byte stateIndex) {
            var state = (State) stateIndex;

            _lastState = state;
        }

        private enum State {
            Closed = 0,
            Open
        }

        private enum Animation {
            Close = 0,
            Open,
            QuickClose,
            QuickOpen
        }
    }
}