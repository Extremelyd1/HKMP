using System.Collections.Generic;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Game.Client.Entity {

    public class MantisLordS1 : MantisLordBase {

        // FSM that decides the two Mantis lords' moves
        private readonly PlayMakerFSM _subFsm;

        public MantisLordS1(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, entityId, gameObject, EntityType.MantisLordS1) {
            _subFsm = GameObject.Find("Battle Sub").LocateMyFSM("Start");
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            base.InternalInitializeAsSceneClient(stateIndex);

            // Disable the decision FSM, this is handled by the host
            RemoveAllTransitions(_subFsm);
        }

        protected override void InternalSwitchToSceneHost() {
            base.InternalSwitchToSceneHost();

            // Reenable the decision FSM
            RestoreAllTransitions(_subFsm);
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            base.UpdateAnimation(animationIndex, animationInfo);
        }
    }

    public class MantisLordS2 : MantisLordBase {
        public MantisLordS2(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, entityId, gameObject, EntityType.MantisLordS2) {
        }
    }

    public class MantisLord : MantisLordBase {
        public MantisLord(
            NetClient netClient,
            byte entityId,
            GameObject gameObject
        ) : base(netClient, entityId, gameObject, EntityType.MantisLord) {
        }
    }

    public class MantisLordBase : HealthManagedEntity {
        private readonly PlayMakerFSM _fsm;
        private Animation _lastAnimation;

        public MantisLordBase(
            NetClient netClient,
            byte entityId,
            GameObject gameObject,
            EntityType entityType
        ) : base(netClient, entityType, entityId, gameObject) {
            _fsm = gameObject.LocateMyFSM("Mantis Lord");

            CreateAnimationEvents();
        }

        private void CreateAnimationEvents() {
            // Dash
            _fsm.InsertMethod("Dash Arrive", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DashArrive); }));

            _fsm.InsertMethod("Dash Antic", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DashAntic); }));

            _fsm.InsertMethod("Dash Attack", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DashAttack); }));

            _fsm.InsertMethod("Dash Recover", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DashRecover); }));

            _fsm.InsertMethod("Dash Leave", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DashLeave); }));

            // Dstab
            _fsm.InsertMethod("Dstab Arrive", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DstabArrive); }));

            _fsm.InsertMethod("Dstab Attack", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DstabAttack); }));

            _fsm.InsertMethod("Dstab Land", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DstabLand); }));

            _fsm.InsertMethod("Dstab Leave", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DstabLeave); }));

            _fsm.InsertMethod("Away", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Away); }));

            // Throw projectile from low position
            _fsm.InsertMethod("Arrive", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Arrive); }));

            _fsm.InsertMethod("Throw Antic", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.ThrowAntic); }));

            _fsm.InsertMethod("Throw", 0, CreateUpdateMethod(() => {
                // Left or right throw
                var throwR = _fsm.FsmVariables.GetFsmBool("Throw R").Value ? 1 : 0;

                var noDust = _fsm.FsmVariables.GetFsmBool("No Dust").Value ? 1 : 0;

                // Whether the CD will be thrown to go from up to down
                var throwUD = _fsm.FsmVariables.GetFsmBool("Throw UD").Value ? 1 : 0;
                SendAnimationUpdate((byte) Animation.Throw, new List<byte> { (byte) throwR, (byte) noDust, (byte) throwUD });
            }));

            // Throw projectile from high position
            _fsm.InsertMethod("Arrive 2", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Arrive2); }));

            _fsm.InsertMethod("Throw Antic 2", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.ThrowAntic2); }));

            _fsm.InsertMethod("Throw 2", 0, CreateUpdateMethod(() => {
                // Set how to throw the projectile
                var shotEventFsm = _fsm.FsmVariables.GetFsmString("Shot Event").Value;
                var shotEvent = ShotEvent.WIDE_L;
                switch (shotEventFsm) {
                    case "WIDE R":
                        shotEvent = ShotEvent.WIDE_R;
                        break;
                    case "NARROW L":
                        shotEvent = ShotEvent.NARROW_L;
                        break;
                    case "NARROW R":
                        shotEvent = ShotEvent.NARROW_R;
                        break;
                }
                SendAnimationUpdate((byte) Animation.Throw2, new List<byte> { (byte) shotEvent });
            }));

            // Wall leave animation (Wall Leave 1 + 2)

            _fsm.InsertMethod("Wall Leave 1", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.WallLeave); }));
        }

        protected override void InternalInitializeAsSceneHost() {
            var activeStateName = _fsm.ActiveStateName;

            switch (activeStateName) {
                case "Pause":
                case "Init":
                case "Start Pause":
                case "Idle":
                    SendStateUpdate((byte) State.Idle);
                    break;
                default:
                    SendStateUpdate((byte) State.Active);
                    break;
            }
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_fsm);
        }

        protected override void InternalSwitchToSceneHost() {
            // Restore the original FSM
            RestoreAllTransitions(_fsm);

            // Set the current state of the FSM based on the last animation
            switch (_lastAnimation) {
                case Animation.DashArrive:
                    _fsm.SetState("Dash Antic");
                    break;
                case Animation.DashAntic:
                    _fsm.SetState("Dash Attack");
                    break;
                case Animation.DashAttack:
                    _fsm.SetState("Dash Recover");
                    break;
                case Animation.DashRecover:
                    _fsm.SetState("Dash Leave");
                    break;
                case Animation.DashLeave:
                    _fsm.SetState("Away");
                    break;
                case Animation.DstabArrive:
                    _fsm.SetState("Dstab Attack");
                    break;
                case Animation.DstabAttack:
                    _fsm.SetState("Dstab Land");
                    break;
                case Animation.DstabLand:
                    _fsm.SetState("Dstab Leave");
                    break;
                case Animation.DstabLeave:
                    _fsm.SetState("Away");
                    break;
                case Animation.Away:
                    _fsm.SetState("Idle");
                    break;
                case Animation.Arrive:
                    _fsm.SetState("Throw Antic");
                    break;
                case Animation.ThrowAntic:
                    _fsm.SetState("Throw");
                    break;
                case Animation.Throw:
                    _fsm.SetState("Wall Leave 1");
                    break;
                case Animation.Arrive2:
                    _fsm.SetState("Throw Antic 2");
                    break;
                case Animation.ThrowAntic2:
                    _fsm.SetState("Throw 2");
                    break;
                case Animation.Throw2:
                    _fsm.SetState("Wall Leave 1");
                    break;
                case Animation.WallLeave:
                    _fsm.SetState("Idle");
                    break;
            }
        }


        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation) animationIndex;

            _lastAnimation = animation;

            Logger.Get().Info(this, $"Received animation: {animation}");

            if (animation == Animation.DashArrive) {
                _fsm.ExecuteActions("Dash Arrive", 1, 2, 3, 4);
            }

            if (animation == Animation.DashAntic) {
                _fsm.ExecuteActions("Dash Antic", 1);
            }

            if (animation == Animation.DashAttack) {
                _fsm.ExecuteActions("Dash Attack", 1, 2, 3, 12);
            }

            if (animation == Animation.DashRecover) {
                _fsm.ExecuteActions("Dash Recover", 2, 4);
            }

            if (animation == Animation.DashLeave) {
                _fsm.ExecuteActions("Dash Leave", 1, 2, 3);
            }

            if (animation == Animation.DstabArrive) {
                _fsm.ExecuteActions("Dstab Arrive", 1, 2, 3, 4);
            }

            if (animation == Animation.DstabAttack) {
                _fsm.ExecuteActions("Dstab Attack", 1, 2);
            }

            if (animation == Animation.DstabLand) {
                _fsm.ExecuteActions("Dstab Land", 1, 4, 5);
            }

            if (animation == Animation.DstabLeave) {
                _fsm.ExecuteActions("Dstab Leave", 1, 2);
            }

            if (animation == Animation.Away) {
                _fsm.ExecuteActions("Away", 2, 3);
            }

            if (animation == Animation.Arrive) {
                _fsm.ExecuteActions("Arrive", 1, 2, 3, 4);
            }

            if (animation == Animation.ThrowAntic) {
                _fsm.ExecuteActions("Throw Antic", 1);
            }

            if (animation == Animation.Throw) {
                _fsm.FsmVariables.GetFsmBool("Throw R").Value = animationInfo[0] == 1;
                _fsm.FsmVariables.GetFsmBool("No Dust").Value = animationInfo[1] == 1;
                _fsm.FsmVariables.GetFsmBool("Throw UD").Value = animationInfo[2] == 1;

                _fsm.ExecuteActions("Throw", 1, 2, 3, 4, 5, 6, 8);
            }

            if (animation == Animation.Arrive2) {
                _fsm.ExecuteActions("Arrive 2", 1, 2, 3, 4);
            }

            if (animation == Animation.ThrowAntic2) {
                // This one is actually the same as "Throw Antic", but separate here for clarity and host switches
                _fsm.ExecuteActions("Throw Antic 2", 1);
            }

            if (animation == Animation.Throw2) {
                // Check to throw the projectile
                var shotEvent = (ShotEvent) animationInfo[0];
                var shotEventFsm = "";

                switch (shotEvent) {
                    case ShotEvent.WIDE_L:
                        shotEventFsm = "WIDE L";
                        break;
                    case ShotEvent.WIDE_R:
                        shotEventFsm = "WIDE R";
                        break;
                    case ShotEvent.NARROW_L:
                        shotEventFsm = "NARROW L";
                        break;
                    case ShotEvent.NARROW_R:
                        shotEventFsm = "NARROW R";
                        break;
                }
                _fsm.FsmVariables.GetFsmString("Shot Event").Value = shotEventFsm;
                _fsm.ExecuteActions("Throw 2", 1, 2, 3, 4);
            }

            if (animation == Animation.WallLeave) {
                _fsm.ExecuteActions("Wall Leave 1", 2);
                _fsm.GetAction<Tk2dPlayAnimationWithEvents>("Wall Leave 1", 1).Execute(() => {
                    _fsm.GetAction<Tk2dPlayAnimationWithEvents>("Wall Leave 2", 0).Execute(() => {
                        _fsm.ExecuteActions("After Throw Pause", 1, 2);
                    });
                });
            }
        }

        public override void UpdateState(byte state) {
        }

        private enum State {
            Idle = 0,
            Active,
        }

        private enum Animation {
            DashArrive = 0,
            DashAntic,
            DashAttack,
            DashRecover,
            DashLeave,
            DstabArrive,
            DstabAttack,
            DstabLand,
            DstabLeave,
            Away,
            Arrive,
            ThrowAntic,
            Throw,
            HighThrow,
            WallLeave,
            Arrive2,
            ThrowAntic2,
            Throw2,
        }

        private enum ShotEvent {
            WIDE_L = 0,
            WIDE_R,
            NARROW_L,
            NARROW_R
        }
    }
}
