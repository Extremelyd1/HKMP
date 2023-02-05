using System;
using System.Collections.Generic;
using Hkmp.Networking.Client;
using Hkmp.Util;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;

namespace Hkmp.Game.Client.Entity;

internal class FalseKnight : Entity {
    private static readonly Dictionary<State, string> SimpleEventStates = new Dictionary<State, string> {
        { State.Fall, "Start Fall" },
        { State.TurnR, "Turn R" },
        { State.TurnL, "Turn L" },
        { State.Run, "Run Antic" },
        { State.JumpAttackRight, "JA Right" },
        { State.JumpAttackLeft, "JA Left" },
        { State.StunTurnL, "Stun Turn L" },
        { State.StunTurnR, "Stun Turn R" },
        { State.StunStart, "Stun Start" },
        { State.OpenUp, "Open Uuup" },
        { State.Hit, "Hit" },
        { State.StunFail, "Stun Fail" },
        { State.Recover, "Recover" },
        { State.ToPhase2, "To Phase 2" },
        { State.ToPhase3, "To Phase 3" },
        { State.JumpAttack2, "JA Antic 2" },
        { State.Hit2, "Hit 2" },
        { State.Death, "Death Anim Start" }
    };

    private static readonly string[] StateUpdateResetNames = {
        // After the initial fall, the FSM will end up here
        "First Idle",
        // Most sequences end back up in the Idle state
        "Idle",
        // The run sequence splits in jumping left or right
        "JA Check Hero Pos",
        // The slam sequences (jump and non-jump) end in "Voice? 2"
        "Voice? 2",
        // The move choice state has a lot of outputs, and some states end up here
        "Move Choice",
        // Part of the stun/stagger sequence, might be transitioned to
        // after a global Stun event
        "Check Direction",
        // The end of the tumble through the air after a stun
        "Roll End",
        // When False Knight's armor is opened and the is popped out
        "Opened",
        // After the ground slamming rage is over
        "Rage Check",
        // When False Knight's falls through the floor and its armor opens up
        "Opened 2"
    };

    private static readonly List<State> InterruptingStates = new List<State> {
        State.StunTurnL,
        State.StunTurnR,
        State.StunStart,
        State.Recover
    };

    private bool _isInitialized;

    public FalseKnight(
        NetClient netClient,
        byte entityId,
        GameObject gameObject
    ) : base(
        netClient,
        EntityType.FalseKnight,
        entityId,
        gameObject
    ) {
        Fsm = gameObject.LocateMyFSM("FalseyControl");

        CreateEvents();
    }

    private void CreateEvents() {
        //
        // Insert methods for sending updates over network for reached states
        //
        foreach (var stateNamePair in SimpleEventStates) {
            Fsm.InsertMethod(stateNamePair.Value, 0, CreateStateUpdateMethod(() => {
                Logger.Info($"Sending {stateNamePair.Key} state");
                SendStateUpdate((byte) stateNamePair.Key);
            }));
        }

        Fsm.InsertMethod("Jump Antic", 0, CreateStateUpdateMethod(() => {
            var variables = new List<byte>();

            // Get the Jump X variable from the FSM and add it as bytes to the variables list
            var jumpXFloat = Fsm.FsmVariables.GetFsmFloat("Jump X").Value;
            variables.AddRange(BitConverter.GetBytes(jumpXFloat));

            Logger.Info($"Sending Jump state with variable: {jumpXFloat}");

            SendStateUpdate((byte) State.Jump, variables);
        }));

        Fsm.InsertMethod("S Jump", 0, CreateStateUpdateMethod(() => {
            var variables = new List<byte>();

            // Get the Jump X variable from the FSM and add it as bytes to the variables list
            var jumpXFloat = Fsm.FsmVariables.GetFsmFloat("Jump X").Value;
            variables.AddRange(BitConverter.GetBytes(jumpXFloat));

            Logger.Info($"Sending Slam Jump state with variable: {jumpXFloat}");

            SendStateUpdate((byte) State.SlamJump, variables);
        }));

        Fsm.InsertMethod("S Attack Antic", 0, CreateStateUpdateMethod(() => {
            var variables = new List<byte>();

            var shockwaveXOriginFloat = Fsm.FsmVariables.GetFsmFloat("Shockwave X Origin").Value;
            variables.AddRange(BitConverter.GetBytes(shockwaveXOriginFloat));

            var shockwaveGoingRightBool = Fsm.FsmVariables.GetFsmBool("Shockwave Going Right").Value;
            variables.AddRange(BitConverter.GetBytes(shockwaveGoingRightBool));

            Logger.Info(
                $"Sending Slam Attack state with variables: {shockwaveXOriginFloat}, {shockwaveGoingRightBool}");
            SendStateUpdate((byte) State.SlamAttack, variables);
        }));

        //
        // Insert methods for resetting the update state, so we can start/receive the next update
        //
        foreach (var stateName in StateUpdateResetNames) {
            Fsm.InsertMethod(stateName, 0, StateUpdateDone);
        }

        Fsm.InsertMethod("Turn R", 8, StateUpdateDone);
        Fsm.InsertMethod("Turn L", 8, StateUpdateDone);
    }

    protected override void InternalTakeControl() {
        foreach (var stateName in StateUpdateResetNames) {
            RemoveOutgoingTransitions(stateName);
        }

        // Make sure that the FSM doesn't even start at all,
        // by removing transitions of one of the first states
        RemoveOutgoingTransitions("Dormant");

        RemoveOutgoingTransition("Hit", "Recover");
    }

    protected override void InternalReleaseControl() {
        RestoreAllOutgoingTransitions();
    }

    protected override void StartQueuedUpdate(byte state, List<byte> variables) {
        var variableArray = variables.ToArray();

        var enumState = (State) state;

        // If we not initialized before this state update, we need to
        // do it before we set the FSM states and variables
        if (!_isInitialized) {
            InitializeForIntermediateState();
        }

        _isInitialized = true;

        if (SimpleEventStates.TryGetValue(enumState, out var stateName)) {
            Logger.Info($"Received {enumState} state");
            Fsm.SetState(stateName);

            return;
        }

        switch ((State) state) {
            case State.Jump:
                var jumpXFloat = BitConverter.ToSingle(variableArray, 0);

                Logger.Info($"Received Jump state with variable: {jumpXFloat}");

                Fsm.FsmVariables.GetFsmFloat("Jump X").Value = jumpXFloat;

                Fsm.SetState("Jump Antic");
                break;
            case State.SlamJump:
                var slamJumpXFloat = BitConverter.ToSingle(variableArray, 0);

                Logger.Info($"Received Slam Jump state with variable: {slamJumpXFloat}");

                Fsm.FsmVariables.GetFsmFloat("Jump X").Value = slamJumpXFloat;

                Fsm.SetState("S Jump");
                break;
            case State.SlamAttack:
                var shockwaveXOriginFloat = BitConverter.ToSingle(variableArray, 0);
                var shockwaveGoingRightBool = BitConverter.ToBoolean(variableArray, 4);

                Logger.Info(
                    $"Received Slam Attack state with variables: {shockwaveXOriginFloat}, {shockwaveGoingRightBool}");

                Fsm.FsmVariables.GetFsmFloat("Shockwave X Origin").Value = shockwaveXOriginFloat;
                Fsm.FsmVariables.GetFsmBool("Shockwave Going Right").Value = shockwaveGoingRightBool;

                Fsm.SetState("S Attack Antic");
                break;
        }
    }

    protected override bool IsInterruptingState(byte state) {
        return InterruptingStates.Contains((State) state);
    }

    private void InitializeForIntermediateState() {
        // The mesh renderer is disabled until the Start Fall state
        GameObject.GetComponent<MeshRenderer>().enabled = true;

        // Same holds for rigidbody properties
        var rigidbody = GameObject.GetComponent<Rigidbody2D>();
        rigidbody.isKinematic = false;
        rigidbody.gravityScale = 1;
    }

    private enum State {
        Fall = 0,
        Jump,
        TurnR,
        TurnL,
        Run,
        JumpAttackRight,
        JumpAttackLeft,
        SlamJump,
        SlamAttack,
        StunTurnL,
        StunTurnR,
        StunStart,
        OpenUp,
        Hit,
        StunFail,
        Recover,
        ToPhase2,
        ToPhase3,
        JumpAttack2,
        Hit2,
        Death,
    }
}
