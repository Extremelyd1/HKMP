using System.Collections.Generic;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace Hkmp.Game.Client.Entity {

    public sealed class MantisLordS1 : MantisLordSub {

        // FSM that decides the two Mantis lords' moves
        private readonly PlayMakerFSM _subFsm;
        // FSM that decides when the sub fight is over
        private readonly PlayMakerFSM _battleFsm;
        private SubBattleAnimation _lastSubBattleAnimation;

        public MantisLordS1(
            NetClient netClient,
            byte entityId,
            GameObject gameObject,
            GameObject throneObject
        ) : base(netClient, entityId, gameObject, throneObject, EntityType.MantisLordS1) {
            _subFsm = GameObject.Find("Battle Sub").LocateMyFSM("Start");
            _battleFsm = GameObject.Find("Mantis Battle").LocateMyFSM("Battle Control");

            CreateAnimationEvents();

            _lastSubBattleAnimation = SubBattleAnimation.Idle;
        }

        protected override void CreateAnimationEvents() {
            base.CreateAnimationEvents();

            _subFsm.InsertMethod("Init", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) SubBattleAnimation.Start); }));

            _subFsm.InsertMethod("Start", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) SubBattleAnimation.Start); }));

            _subFsm.InsertMethod("Killed", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) SubBattleAnimation.Killed);

                SendStateUpdate((byte) ThroneState.EmptyKilled);
            }));

            _battleFsm.InsertMethod("Journal", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) SubBattleAnimation.KilledBoth);

                SendStateUpdate((byte) ThroneState.DefeatedBoth);
            }));
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            // Disable the decision FSM, this is handled by the host
            RemoveAllTransitions(_subFsm);

            if (stateIndex.HasValue) {
                var state = (ThroneState) stateIndex;

                if (state == ThroneState.Empty) {
                    _subFsm.ExecuteActions("Init", 1, 2, 3);
                    _subFsm.ExecuteActions("Start", 1);

                    _subFsm.SetState("Init Pause");
                }

                if (state == ThroneState.EmptyKilled) {
                    _subFsm.ExecuteActions("Init", 1, 2, 3);
                    _subFsm.ExecuteActions("Start", 1);
                    _subFsm.ExecuteActions("Killed", 1, 2);

                    _subFsm.SetState("Killed");
                }

                if (state == ThroneState.DefeatedBoth) {
                    var battleEnemies = _battleFsm.FsmVariables.GetFsmInt("Battle Enemies").Value;

                    // Force the end of the fight
                    if (battleEnemies > 0) {
                        _battleFsm.SetState("Journal");
                    }
                }
            }

            // Now initialize the base class, as we might have activated its object
            base.InternalInitializeAsSceneClient(stateIndex);
        }

        protected override void InternalSwitchToSceneHost() {
            base.InternalSwitchToSceneHost();

            // Reenable the decision FSM
            RestoreAllTransitions(_subFsm);

            // If the host leaves and the client has joined in the second phase, we need to trigger the actions again
            if (_lastSubBattleAnimation == SubBattleAnimation.Idle && _subFsm.ActiveStateName == "Init Pause") {
                _subFsm.SetState("Init Pause");
            }

            if (_lastSubBattleAnimation == SubBattleAnimation.Init) {
                _subFsm.SetState("Start");
            }

            if (_lastSubBattleAnimation == SubBattleAnimation.Start) {
                _subFsm.SetState("Set Subs");
            }

            if (_lastSubBattleAnimation == SubBattleAnimation.Killed) {
                _subFsm.SetState("Killed");
            }
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            if (animationIndex <= (byte) ThroneAnimation.Return || animationIndex == 255) {
                base.UpdateAnimation(animationIndex, animationInfo);
                return;
            }

            var animation = (SubBattleAnimation) animationIndex;

            _lastSubBattleAnimation = animation;

            if (animation == SubBattleAnimation.Init) {
                _subFsm.ExecuteActions("Init", 1, 2, 3);
            }

            if (animation == SubBattleAnimation.Start) {
                _subFsm.ExecuteActions("Start", 1);
            }

            if (animation == SubBattleAnimation.Killed) {
                _subFsm.ExecuteActions("Killed", 1, 2);
            }

            if (animation == SubBattleAnimation.KilledBoth) {
                var battleEnemies = _battleFsm.FsmVariables.GetFsmInt("Battle Enemies").Value;

                // Force the end of the fight
                if (battleEnemies > 0) {
                    _battleFsm.SetState("Journal");
                }
            }
        }

        private enum SubBattleAnimation {
            Idle = ThroneAnimation.Return + 1,
            Init,
            Start,
            Killed,
            KilledBoth,
        }
    }

    public sealed class MantisLordS2 : MantisLordSub {
        public MantisLordS2(
            NetClient netClient,
            byte entityId,
            GameObject gameObject,
            GameObject throneObject
        ) : base(netClient, entityId, gameObject, throneObject, EntityType.MantisLordS2) {
            CreateAnimationEvents();
        }
    }

    public abstract class MantisLordSub : MantisLordBase {

        private readonly PlayMakerFSM _throneFsm;
        private ThroneAnimation _lastThroneAnimation;

        protected MantisLordSub(
            NetClient netClient,
            byte entityId,
            GameObject gameObject,
            GameObject throneObject,
            EntityType entityType
        ) : base(netClient, entityId, gameObject, entityType) {
            _throneFsm = throneObject.LocateMyFSM("Mantis Throne Sub");
        }

        protected override void CreateAnimationEvents() {
            base.CreateAnimationEvents();

            _throneFsm.InsertMethod("Stand", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) ThroneAnimation.Stand);

                SendStateUpdate((byte) ThroneState.Standing);
            }));

            _throneFsm.InsertMethod("Leave", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) ThroneAnimation.Leave);

                SendStateUpdate((byte) ThroneState.Empty);
            }));

            _throneFsm.InsertMethod("Fighting", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) ThroneAnimation.Fighting); }));

            _throneFsm.InsertMethod("Return", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) ThroneAnimation.Return);

                SendStateUpdate((byte) ThroneState.Defeated);
            }));
        }

        protected override void InternalInitializeAsSceneHost() {
            base.InternalInitializeAsSceneHost();

            var activeStateName = _throneFsm.ActiveStateName;

            switch (activeStateName) {
                case "Init":
                case "Idle":
                    SendStateUpdate((byte) ThroneState.Idle);
                    break;
                case "Stand":
                    SendStateUpdate((byte) ThroneState.Standing);
                    break;
                case "Leave":
                case "Fighting":
                    SendStateUpdate((byte) ThroneState.Empty);
                    break;
                case "Return Pause":
                case "Return":
                    SendStateUpdate((byte) ThroneState.Defeated);
                    break;
            }
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_throneFsm);

            // Determine the base state class state using the throne state
            var baseStateIndex = (byte) State.Idle;
            if (stateIndex.HasValue) {
                var state = (ThroneState) stateIndex;

                Logger.Get().Info(this, $"Initializing with state: {state}");

                if (state == ThroneState.Standing) {
                    _throneFsm.ExecuteActions("Stand", 1);
                    _throneFsm.SetState("Stand");
                }

                if (state == ThroneState.Empty || state == ThroneState.EmptyKilled) {
                    // The base mantis lord object is active when the throne is empty
                    baseStateIndex = (byte) State.Active;
                    _throneFsm.ExecuteActions("Fighting", 1);
                    _throneFsm.SetState("Fighting");
                }

                if (state == ThroneState.Defeated || state == ThroneState.DefeatedBoth) {
                    _throneFsm.ExecuteActions("Return", 2, 3);
                    _throneFsm.SetState("Return");

                    var healthManager = GameObject.GetComponent<HealthManager>();
                    if (!healthManager.GetIsDead()) {
                        GameObject.SetActive(false);
                    }
                }
            }

            // Now call the base class initializer, as we might have activated the mantis lord object
            base.InternalInitializeAsSceneClient(baseStateIndex);
        }

        protected override void InternalSwitchToSceneHost() {
            base.InternalSwitchToSceneHost();

            RestoreAllTransitions(_throneFsm);

            switch (_lastThroneAnimation) {
                case ThroneAnimation.Stand:
                    _throneFsm.SetState("Leave");
                    break;
                case ThroneAnimation.Leave:
                    _throneFsm.SetState("Fighting");
                    break;
                case ThroneAnimation.Fighting:
                    var healthManager = GameObject.GetComponent<HealthManager>();
                    // We might have never received the defeated event, but the entity is dead already
                    if (healthManager.GetIsDead()) {
                        _throneFsm.SetState("Return Pause");
                    } else {
                        _throneFsm.SetState("Fighting");
                    }
                    break;
            }
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            if (animationIndex <= (byte) Animation.Throw2 || animationIndex == 255) {
                base.UpdateAnimation(animationIndex, animationInfo);
                return;
            }

            var animation = (ThroneAnimation) animationIndex;

            _lastThroneAnimation = animation;

            Logger.Get().Info(this, $"Received animation: {animation}");

            if (animation == ThroneAnimation.Stand) {
                _throneFsm.ExecuteActions("Stand", 1);
            }

            if (animation == ThroneAnimation.Leave) {
                _throneFsm.ExecuteActions("Leave", 1, 2);
            }

            if (animation == ThroneAnimation.Fighting) {
                _throneFsm.ExecuteActions("Fighting", 1);
            }

            if (animation == ThroneAnimation.Return) {
                _throneFsm.ExecuteActions("Return", 1, 2, 3);
            }
        }

        protected enum ThroneAnimation {
            Stand = Animation.Throw2 + 1,
            Leave,
            Fighting,
            Return,
        }

        protected enum ThroneState {
            Idle = 0,
            Standing,
            Empty,
            EmptyKilled,
            Defeated,
            DefeatedBoth,
        }
    }

    public sealed class MantisLord : MantisLordBase {

        private readonly PlayMakerFSM _throneFsm;
        // Fsm that control the challenge prompt, which needs to be disabled on the client
        private readonly PlayMakerFSM _challengePromptFsm;
        private ThroneAnimation _lastThroneAnimation;
        private readonly bool _godHome;

        public MantisLord(
            NetClient netClient,
            byte entityId,
            GameObject gameObject,
            GameObject throneObject,
            GameObject challengePromptObject
        ) : base(netClient, entityId, gameObject, EntityType.MantisLord) {
            _throneFsm = throneObject.LocateMyFSM("Mantis Throne Main");

            if (challengePromptObject) {
                _challengePromptFsm = challengePromptObject.LocateMyFSM("Challenge Start");
                _godHome = false;
            } else {
                _godHome = true;
            }

            CreateAnimationEvents();
        }

        protected override void CreateAnimationEvents() {
            base.CreateAnimationEvents();

            // This is inserted at index 1, so it only sends if the fight is not in Godhome
            _throneFsm.InsertMethod("Music", 1,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) ThroneAnimation.Music); }));

            _throneFsm.InsertMethod("Stand", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) ThroneAnimation.Stand);

                SendStateUpdate((byte) ThroneState.Stand);
            }));

            _throneFsm.InsertMethod("Cage", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) ThroneAnimation.Cage);

                SendStateUpdate((byte) ThroneState.Cage);
            }));

            _throneFsm.InsertMethod("Floors", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) ThroneAnimation.Floors);

                SendStateUpdate((byte) ThroneState.Floors);
            }));

            // Things to do with the challenge are ignored, as everyone in the scene would lose control of their character
            _throneFsm.InsertMethod("Leave", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) ThroneAnimation.Leave);

                SendStateUpdate((byte) ThroneState.Empty);
            }));

            _throneFsm.InsertMethod("Wake", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) ThroneAnimation.Wake); }));

            _throneFsm.InsertMethod("Defeated", 0, CreateUpdateMethod(() => {
                SendAnimationUpdate((byte) ThroneAnimation.Defeated);

                SendStateUpdate((byte) ThroneState.Defeated);
            }));
        }

        protected override void InternalInitializeAsSceneHost() {
            base.InternalInitializeAsSceneHost();

            var activeStateName = _throneFsm.ActiveStateName;

            switch (activeStateName) {
                case "Init":
                case "Idle":
                    SendStateUpdate((byte) ThroneState.Idle);
                    break;
                case "Music":
                case "Stand":
                    SendStateUpdate((byte) ThroneState.Stand);
                    break;
                case "Cage":
                    SendStateUpdate((byte) ThroneState.Cage);
                    break;
                case "Floors":
                    SendStateUpdate((byte) ThroneState.Floors);
                    break;
                case "End Challenge":
                case "Flip Back":
                case "Regain Control":
                case "Leave":
                case "Wake":
                    SendStateUpdate((byte) ThroneState.Empty);
                    break;
                case "Pause":
                case "Defeated":
                case "Start Sub":
                    SendStateUpdate((byte) ThroneState.Defeated);
                    break;
            }
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_throneFsm);
            if (!_godHome) {
                RemoveAllTransitions(_challengePromptFsm);
            }

            // Determine the base state class state using the throne state
            var baseStateIndex = new byte?();
            if (stateIndex.HasValue) {
                baseStateIndex = stateIndex == (byte) ThroneState.Empty ? (byte) State.Active : (byte) State.Idle;
                var state = (ThroneState) stateIndex;

                Logger.Get().Info(this, $"Initializing with state: {state}");

                if (state == ThroneState.Stand) {
                    if (!_godHome) {
                        _challengePromptFsm.ExecuteActions("Send Challenge", 1);
                        _throneFsm.ExecuteActions("Music", 2, 3);
                    }
                    _throneFsm.ExecuteActions("Stand", 1);

                    _throneFsm.SetState("Stand");
                }

                if (state == ThroneState.Cage) {
                    if (!_godHome) {
                        _challengePromptFsm.ExecuteActions("Send Challenge", 1);
                        _throneFsm.ExecuteActions("Music", 2, 3);
                    }
                    _throneFsm.ExecuteActions("Stand", 1);
                    _throneFsm.ExecuteActions("Cage", 1, 2, 3, 4, 5);

                    _throneFsm.SetState("Cage");
                }

                if (state == ThroneState.Floors) {
                    if (!_godHome) {
                        _challengePromptFsm.ExecuteActions("Send Challenge", 1);
                        _throneFsm.ExecuteActions("Music", 2, 3);
                    }
                    _throneFsm.ExecuteActions("Stand", 1);
                    _throneFsm.ExecuteActions("Cage", 1, 2, 3, 4, 5);
                    _throneFsm.ExecuteActions("Floors", 1);

                    _throneFsm.SetState("Floors");
                }

                if (state == ThroneState.Empty) {
                    if (!_godHome) {
                        _challengePromptFsm.ExecuteActions("Send Challenge", 1);
                        _throneFsm.ExecuteActions("Music", 2, 3);
                    }
                    _throneFsm.ExecuteActions("Cage", 1, 2, 3, 4, 5);
                    _throneFsm.ExecuteActions("Floors", 1);
                    _throneFsm.ExecuteActions("Leave", 1, 2);
                    _throneFsm.ExecuteActions("Wake", 1, 2);

                    _throneFsm.SetState("Wake");
                }

                if (state == ThroneState.Defeated) {
                    if (!_godHome) {
                        _challengePromptFsm.ExecuteActions("Send Challenge", 1);
                        _throneFsm.ExecuteActions("Music", 2, 3);
                    }
                    _throneFsm.ExecuteActions("Cage", 1, 2, 3, 4, 5);
                    _throneFsm.ExecuteActions("Floors", 1);
                    _throneFsm.ExecuteActions("Defeated", 1);

                    _throneFsm.SetState("Defeated");
                }
            }

            // Now call the base class initializer, as we might have activated the mantis lord object
            base.InternalInitializeAsSceneClient(baseStateIndex);
        }

        protected override void InternalSwitchToSceneHost() {
            base.InternalSwitchToSceneHost();

            RestoreAllTransitions(_throneFsm);
            if (!_godHome) {
                RestoreAllTransitions(_challengePromptFsm);
            }

            switch (_lastThroneAnimation) {
                case ThroneAnimation.Music:
                    _throneFsm.SetState("Stand");
                    break;
                case ThroneAnimation.Stand:
                    _throneFsm.SetState("Cage");
                    break;
                case ThroneAnimation.Cage:
                    _throneFsm.SetState("Floors");
                    break;
                case ThroneAnimation.Floors:
                    _throneFsm.SetState("Leave");
                    break;
                case ThroneAnimation.Leave:
                    _throneFsm.SetState("Wake");
                    break;
                case ThroneAnimation.Wake:
                    var healthManager = GameObject.GetComponent<HealthManager>();
                    // We might have never received the defeated event, but the entity is dead already
                    if (healthManager.GetIsDead()) {
                        _throneFsm.SetState("Pause");
                    } else {
                        _throneFsm.SetState("Wake");
                    }
                    break;
                case ThroneAnimation.Defeated:
                    _throneFsm.SetState("Start Sub");
                    break;
            }
        }

        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            if (animationIndex <= (byte) Animation.Throw2 || animationIndex == 255) {
                base.UpdateAnimation(animationIndex, animationInfo);
                return;
            }

            var animation = (ThroneAnimation) animationIndex;

            _lastThroneAnimation = animation;

            Logger.Get().Info(this, $"Received animation: {animation}");

            if (animation == ThroneAnimation.Music) {
                // Send the challenge event to stop the Deepnest gate from triggering
                _challengePromptFsm.ExecuteActions("Send Challenge", 0);
                _throneFsm.ExecuteActions("Music", 2, 3);
            }

            if (animation == ThroneAnimation.Stand) {
                // Not sure if we can find some way to enable cam lock here.
                // For now we don't as we don't want everyone in the scene to have cam lock...
                _throneFsm.ExecuteActions("Stand", 1, 4);
            }

            if (animation == ThroneAnimation.Cage) {
                // Same here with the area title, although this is not so annoying for the others
                _throneFsm.ExecuteActions("Cage", 1, 2, 3, 4, 5);
            }

            if (animation == ThroneAnimation.Floors) {
                _throneFsm.ExecuteActions("Floors", 1);
            }

            if (animation == ThroneAnimation.Leave) {
                _throneFsm.ExecuteActions("Leave", 1, 2);
            }

            if (animation == ThroneAnimation.Wake) {
                _throneFsm.ExecuteActions("Wake", 1, 2);
            }

            if (animation == ThroneAnimation.Defeated) {
                _throneFsm.ExecuteActions("Defeated", 1, 2, 4);
            }
        }

        private enum ThroneAnimation {
            Music = Animation.Throw2 + 1,
            Stand,
            Cage,
            Floors,
            Leave,
            Wake,
            Defeated,
        }

        private enum ThroneState {
            Idle = 0,
            Stand,
            Cage,
            Floors,
            Empty,
            Defeated,
        }
    }

    public abstract class MantisLordBase : HealthManagedEntity {
        private readonly PlayMakerFSM _mantisFsm;
        private Animation _lastAnimation;

        protected MantisLordBase(
            NetClient netClient,
            byte entityId,
            GameObject mantisObject,
            EntityType entityType
        ) : base(netClient, entityType, entityId, mantisObject) {
            _mantisFsm = mantisObject.LocateMyFSM("Mantis Lord");
        }

        protected virtual void CreateAnimationEvents() {
            _mantisFsm.InsertMethod("Init", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Init); }));

            // Dash
            _mantisFsm.InsertMethod("Dash Arrive", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DashArrive); }));

            _mantisFsm.InsertMethod("Dash Antic", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DashAntic); }));

            _mantisFsm.InsertMethod("Dash Attack", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DashAttack); }));

            _mantisFsm.InsertMethod("Dash Recover", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DashRecover); }));

            _mantisFsm.InsertMethod("Dash Leave", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DashLeave); }));

            // Dstab
            _mantisFsm.InsertMethod("Dstab Arrive", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DstabArrive); }));

            _mantisFsm.InsertMethod("Dstab Attack", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DstabAttack); }));

            _mantisFsm.InsertMethod("Dstab Land", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DstabLand); }));

            _mantisFsm.InsertMethod("Dstab Leave", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.DstabLeave); }));

            _mantisFsm.InsertMethod("Away", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Away); }));

            // Throw projectile from low position
            _mantisFsm.InsertMethod("Arrive", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Arrive); }));

            _mantisFsm.InsertMethod("Throw Antic", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.ThrowAntic); }));

            _mantisFsm.InsertMethod("Throw", 0, CreateUpdateMethod(() => {
                // Left or right throw
                var throwR = _mantisFsm.FsmVariables.GetFsmBool("Throw R").Value ? 1 : 0;

                var noDust = _mantisFsm.FsmVariables.GetFsmBool("No Dust").Value ? 1 : 0;

                // Whether the CD will be thrown to go from up to down
                var throwUD = _mantisFsm.FsmVariables.GetFsmBool("Throw UD").Value ? 1 : 0;
                SendAnimationUpdate((byte) Animation.Throw, new List<byte> { (byte) throwR, (byte) noDust, (byte) throwUD });
            }));

            // Throw projectile from high position
            _mantisFsm.InsertMethod("Arrive 2", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.Arrive2); }));

            _mantisFsm.InsertMethod("Throw Antic 2", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.ThrowAntic2); }));

            _mantisFsm.InsertMethod("Throw 2", 0, CreateUpdateMethod(() => {
                // Set how to throw the projectile
                var shotEventFsm = _mantisFsm.FsmVariables.GetFsmString("Shot Event").Value;
                var shotEvent = ShotEvent.WideL;
                switch (shotEventFsm) {
                    case "WIDE R":
                        shotEvent = ShotEvent.WideR;
                        break;
                    case "NARROW L":
                        shotEvent = ShotEvent.NarrowL;
                        break;
                    case "NARROW R":
                        shotEvent = ShotEvent.NarrowR;
                        break;
                }
                SendAnimationUpdate((byte) Animation.Throw2, new List<byte> { (byte) shotEvent });
            }));

            // Wall leave animation (Wall Leave 1 + 2)

            _mantisFsm.InsertMethod("Wall Leave 1", 0,
                CreateUpdateMethod(() => { SendAnimationUpdate((byte) Animation.WallLeave); }));
        }

        protected override void InternalInitializeAsSceneHost() {
        }

        protected override void InternalInitializeAsSceneClient(byte? stateIndex) {
            RemoveAllTransitions(_mantisFsm);

            if (stateIndex.HasValue) {
                var state = (State) stateIndex;

                if (state == State.Active) {
                    // Set the local gameobject variables for the particle effects and when we switch to host
                    _mantisFsm.ExecuteActions("Init", 1, 2, 3, 4);
                    _mantisFsm.SetState("Init");
                }
            }
        }

        protected override void InternalSwitchToSceneHost() {
            // Restore the original FSM
            RestoreAllTransitions(_mantisFsm);

            // Set the current state of the FSM based on the last animation
            switch (_lastAnimation) {
                case Animation.Init:
                    _mantisFsm.SetState("Start Pause");
                    break;
                case Animation.DashArrive:
                    _mantisFsm.SetState("Dash Antic");
                    break;
                case Animation.DashAntic:
                    _mantisFsm.SetState("Dash Attack");
                    break;
                case Animation.DashAttack:
                    _mantisFsm.SetState("Dash Recover");
                    break;
                case Animation.DashRecover:
                    _mantisFsm.SetState("Dash Leave");
                    break;
                case Animation.DashLeave:
                    _mantisFsm.SetState("Away");
                    break;
                case Animation.DstabArrive:
                    _mantisFsm.SetState("Dstab Attack");
                    break;
                case Animation.DstabAttack:
                    _mantisFsm.SetState("Dstab Land");
                    break;
                case Animation.DstabLand:
                    _mantisFsm.SetState("Dstab Leave");
                    break;
                case Animation.DstabLeave:
                    _mantisFsm.SetState("Away");
                    break;
                case Animation.Away:
                    _mantisFsm.SetState("Idle");
                    break;
                case Animation.Arrive:
                    _mantisFsm.SetState("Throw Antic");
                    break;
                case Animation.ThrowAntic:
                    _mantisFsm.SetState("Throw");
                    break;
                case Animation.Throw:
                    _mantisFsm.SetState("Wall Leave 1");
                    break;
                case Animation.Arrive2:
                    _mantisFsm.SetState("Throw Antic 2");
                    break;
                case Animation.ThrowAntic2:
                    _mantisFsm.SetState("Throw 2");
                    break;
                case Animation.Throw2:
                    _mantisFsm.SetState("Wall Leave 1");
                    break;
                case Animation.WallLeave:
                    _mantisFsm.SetState("Idle");
                    break;
            }
        }


        public override void UpdateAnimation(byte animationIndex, byte[] animationInfo) {
            base.UpdateAnimation(animationIndex, animationInfo);

            var animation = (Animation) animationIndex;

            _lastAnimation = animation;

            Logger.Get().Info(this, $"Received animation: {animation}");

            if (animation == Animation.Init) {
                // Set the local gameobject variables for the particle effects and when we switch to host
                _mantisFsm.ExecuteActions("Init", 1, 2, 3, 4);
            }

            if (animation == Animation.DashArrive) {
                _mantisFsm.ExecuteActions("Dash Arrive", 1, 2, 3, 4);
            }

            if (animation == Animation.DashAntic) {
                _mantisFsm.ExecuteActions("Dash Antic", 1);
            }

            if (animation == Animation.DashAttack) {
                // TODO: Might have to activate ActivateGameObject (Dash hit) here
                _mantisFsm.ExecuteActions("Dash Attack", 1, 2, 3);
            }

            if (animation == Animation.DashRecover) {
                _mantisFsm.ExecuteActions("Dash Recover", 2, 4);
            }

            if (animation == Animation.DashLeave) {
                _mantisFsm.ExecuteActions("Dash Leave", 1, 2, 3);
            }

            if (animation == Animation.DstabArrive) {
                _mantisFsm.ExecuteActions("Dstab Arrive", 1, 2, 3, 4);
            }

            if (animation == Animation.DstabAttack) {
                _mantisFsm.ExecuteActions("Dstab Attack", 1, 2);
            }

            if (animation == Animation.DstabLand) {
                _mantisFsm.ExecuteActions("Dstab Land", 1, 4, 5);
            }

            if (animation == Animation.DstabLeave) {
                _mantisFsm.ExecuteActions("Dstab Leave", 1, 2);
            }

            if (animation == Animation.Away) {
                _mantisFsm.ExecuteActions("Away", 2, 3);
            }

            if (animation == Animation.Arrive) {
                _mantisFsm.ExecuteActions("Arrive", 1, 2, 3, 4);
            }

            if (animation == Animation.ThrowAntic) {
                _mantisFsm.ExecuteActions("Throw Antic", 1);
            }

            if (animation == Animation.Throw) {
                _mantisFsm.FsmVariables.GetFsmBool("Throw R").Value = animationInfo[0] == 1;
                _mantisFsm.FsmVariables.GetFsmBool("No Dust").Value = animationInfo[1] == 1;
                _mantisFsm.FsmVariables.GetFsmBool("Throw UD").Value = animationInfo[2] == 1;

                _mantisFsm.ExecuteActions("Throw", 1, 2, 3, 4, 5, 6, 8);
            }

            if (animation == Animation.Arrive2) {
                _mantisFsm.ExecuteActions("Arrive 2", 1, 2, 3, 4);
            }

            if (animation == Animation.ThrowAntic2) {
                // This one is actually the same as "Throw Antic", but separate here for clarity and host switches
                _mantisFsm.ExecuteActions("Throw Antic 2", 1);
            }

            if (animation == Animation.Throw2) {
                // Check to throw the projectile
                var shotEvent = (ShotEvent) animationInfo[0];
                var shotEventFsm = "";

                switch (shotEvent) {
                    case ShotEvent.WideL:
                        shotEventFsm = "WIDE L";
                        break;
                    case ShotEvent.WideR:
                        shotEventFsm = "WIDE R";
                        break;
                    case ShotEvent.NarrowL:
                        shotEventFsm = "NARROW L";
                        break;
                    case ShotEvent.NarrowR:
                        shotEventFsm = "NARROW R";
                        break;
                }
                _mantisFsm.FsmVariables.GetFsmString("Shot Event").Value = shotEventFsm;
                _mantisFsm.ExecuteActions("Throw 2", 1, 2, 3, 4);
            }

            if (animation == Animation.WallLeave) {
                _mantisFsm.ExecuteActions("Wall Leave 1", 2);
                _mantisFsm.GetAction<Tk2dPlayAnimationWithEvents>("Wall Leave 1", 1).Execute(() => {
                    _mantisFsm.GetAction<Tk2dPlayAnimationWithEvents>("Wall Leave 2", 0).Execute(() => {
                        _mantisFsm.ExecuteActions("After Throw Pause", 1, 2);
                    });
                });
            }
        }

        public override void UpdateState(byte state) {
        }

        protected enum State {
            Idle = 0,
            Active
        }

        protected enum Animation {
            Init = 0,
            DashArrive,
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
            WallLeave,
            Arrive2,
            ThrowAntic2,
            Throw2,
        }

        private enum ShotEvent {
            WideL = 0,
            WideR,
            NarrowL,
            NarrowR
        }
    }
}
