using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Fsm;
using Hkmp.Networking.Client;
using Hkmp.Util;
using HutongGames.PlayMaker;
using UnityEngine;
using Logger = Hkmp.Logging.Logger;
using Vector2 = Hkmp.Math.Vector2;

namespace Hkmp.Game.Client.Entity;

internal abstract class Entity : IEntity {
    private readonly NetClient _netClient;
    private readonly EntityType _entityType;
    private readonly byte _entityId;

    private readonly Queue<StateVariableUpdate> _stateVariableUpdates;
    private bool _inUpdateState;

    protected readonly GameObject GameObject;

    public bool IsControlled { get; private set; }
    public bool AllowEventSending { get; set; }

    // Dictionary containing per state name an array of transitions that the state normally has
    // This is used to revert nulling out the transitions to prevent it from continuing
    private readonly Dictionary<string, FsmTransition[]> _stateTransitions;

    protected PlayMakerFSM Fsm;

    protected Entity(
        NetClient netClient,
        EntityType entityType,
        byte entityId,
        GameObject gameObject
    ) {
        _netClient = netClient;
        _entityType = entityType;
        _entityId = entityId;
        GameObject = gameObject;

        _stateVariableUpdates = new Queue<StateVariableUpdate>();

        _stateTransitions = new Dictionary<string, FsmTransition[]>();

        // Add a position interpolation component to the enemy so we can smooth out position updates
        GameObject.AddComponent<PositionInterpolation>();

        // Register an update event to send position updates
        MonoBehaviourUtil.Instance.OnUpdateEvent += OnUpdate;
    }

    private void OnUpdate() {
        // We don't send updates when this FSM is controlled or when we are not allowed to send events yet
        if (IsControlled || !AllowEventSending) {
            return;
        }

        var transformPos = GameObject.transform.position;

        _netClient.UpdateManager.UpdateEntityPosition(
            _entityType,
            _entityId,
            new Vector2(transformPos.x, transformPos.y)
        );
    }

    public void TakeControl() {
        if (IsControlled) {
            return;
        }

        IsControlled = true;

        InternalTakeControl();
    }

    protected abstract void InternalTakeControl();

    public void ReleaseControl() {
        if (!IsControlled) {
            return;
        }

        IsControlled = false;

        InternalReleaseControl();
    }

    protected abstract void InternalReleaseControl();

    public void UpdatePosition(Vector2 position) {
        var unityPos = new Vector3(position.X, position.Y);

        GameObject.GetComponent<PositionInterpolation>().SetNewPosition(unityPos);
    }

    public void UpdateState(byte state, List<byte> variables) {
        if (IsInterruptingState(state)) {
            Logger.Info("Received update is interrupting state, starting update");

            _inUpdateState = true;

            // Since we interrupt everything that was going on, we can clear the existing queue
            _stateVariableUpdates.Clear();

            StartQueuedUpdate(state, variables);

            return;
        }

        if (!_inUpdateState) {
            Logger.Info("Queue is empty, starting new update");

            _inUpdateState = true;

            // If we are not currently updating the state, we can queue it immediately
            StartQueuedUpdate(state, variables);

            return;
        }

        Logger.Info("Queue is non-empty, queueing new update");

        // There is already an update running, so we queue this one
        _stateVariableUpdates.Enqueue(new StateVariableUpdate {
            State = state,
            Variables = variables
        });
    }

    /**
         * Called when the previous state update is done.
         * Usually called on specific points in the entity's FSM.
         */
    protected void StateUpdateDone() {
        // If the queue is empty when we are done, we reset the boolean
        // so that a new state update can be started immediately
        if (_stateVariableUpdates.Count == 0) {
            Logger.Info("Queue is empty");
            _inUpdateState = false;
            return;
        }

        Logger.Info("Queue is non-empty, starting next");

        // Get the next queued update and start it
        var stateVariableUpdate = _stateVariableUpdates.Dequeue();
        StartQueuedUpdate(stateVariableUpdate.State, stateVariableUpdate.Variables);
    }

    /**
         * Start a (previously queued) update with given state index and variable list.
         */
    protected abstract void StartQueuedUpdate(byte state, List<byte> variable);

    /**
         * Whether the given state index represents a state that should interrupt
         * other updating states.
         */
    protected abstract bool IsInterruptingState(byte state);

    public void Destroy() {
        AllowEventSending = false;

        MonoBehaviourUtil.Instance.OnUpdateEvent -= OnUpdate;
    }

    protected void SendStateUpdate(byte state) {
        _netClient.UpdateManager.UpdateEntityState(_entityType, _entityId, state);
    }

    protected void SendStateUpdate(byte state, List<byte> variables) {
        _netClient.UpdateManager.UpdateEntityStateAndVariables(_entityType, _entityId, state, variables);
    }

    protected void RemoveOutgoingTransitions(string stateName) {
        _stateTransitions[stateName] = Fsm.GetState(stateName).Transitions;

        foreach (var transition in _stateTransitions[stateName]) {
            Logger.Info($"Removing transition in state: {stateName}, to: {transition.ToState}");
        }

        Fsm.GetState(stateName).Transitions = new FsmTransition[0];
    }

    protected void RemoveOutgoingTransition(string stateName, string toState) {
        // Get the current array of transitions
        var originalTransitions = Fsm.GetState(stateName).Transitions;

        // We don't want to overwrite the originally stored transitions,
        // so we only store it if the key doesn't exist yet
        if (!_stateTransitions.TryGetValue(stateName, out _)) {
            _stateTransitions[stateName] = originalTransitions;
        }

        // Try to find the transition that has a destination state with the given name
        var newTransitions = originalTransitions.ToList();
        foreach (var transition in originalTransitions) {
            if (transition.ToState.Equals(toState)) {
                newTransitions.Remove(transition);
                break;
            }
        }

        Fsm.GetState(stateName).Transitions = newTransitions.ToArray();
    }

    protected Action CreateStateUpdateMethod(Action action) {
        return () => {
            if (IsControlled || !AllowEventSending) {
                return;
            }

            action.Invoke();
        };
    }

    protected void RestoreAllOutgoingTransitions() {
        foreach (var stateTransitionPair in _stateTransitions) {
            Fsm.GetState(stateTransitionPair.Key).Transitions = stateTransitionPair.Value;
        }

        _stateTransitions.Clear();
    }

    protected void RestoreOutgoingTransitions(string stateName) {
        if (!_stateTransitions.TryGetValue(stateName, out var transitions)) {
            Logger.Warn($"Tried to restore transitions for state named: {stateName}, but they are not stored");
            return;
        }

        Fsm.GetState(stateName).Transitions = transitions;
        _stateTransitions.Remove(stateName);
    }

    private class StateVariableUpdate {
        public byte State { get; set; }
        public List<byte> Variables { get; set; }
    }
}
