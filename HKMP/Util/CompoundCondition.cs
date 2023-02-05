using System;

namespace Hkmp.Util;

/// <summary>
/// A compound condition consisting of multiple booleans that influence whether something compound is true or not.
/// </summary>
internal class CompoundCondition {
    /// <summary>
    /// The action to execute when the compound condition becomes true.
    /// </summary>
    private readonly Action _enableAction;

    /// <summary>
    /// The action to execute when the compound condition becomes false.
    /// </summary>
    private readonly Action _disableAction;

    /// <summary>
    /// The array of booleans that this compound condition consists of.
    /// </summary>
    private readonly bool[] _conditions;

    /// <summary>
    /// Constructs the condition with the enable action, disable action and the specified number of conditions.
    /// </summary>
    /// <param name="enableAction">The enable action.</param>
    /// <param name="disableAction">The disable action.</param>
    /// <param name="numConditions">The number of conditions initialized with default values.</param>
    public CompoundCondition(Action enableAction, Action disableAction, int numConditions) {
        _enableAction = enableAction;
        _disableAction = disableAction;
        _conditions = new bool[numConditions];
    }

    /// <summary>
    /// Constructs the condition with the enable action, disable action and the given array of initial conditions.
    /// </summary>
    /// <param name="enableAction">The enable action.</param>
    /// <param name="disableAction">The disable action.</param>
    /// <param name="initialConditions">Boolean array containing initial conditions.</param>
    public CompoundCondition(Action enableAction, Action disableAction, params bool[] initialConditions) {
        _enableAction = enableAction;
        _disableAction = disableAction;
        _conditions = initialConditions;
    }

    /// <summary>
    /// Set the condition of an individual boolean at the given index. Will execute the enable or disable
    /// action if the compound condition becomes true or false respectively due to this change.
    /// </summary>
    /// <param name="conditionIndex">The index of the boolean condition.</param>
    /// <param name="value">The new boolean value of the condition.</param>
    public void SetCondition(int conditionIndex, bool value) {
        if (conditionIndex < 0 || conditionIndex >= _conditions.Length) {
            return;
        }

        var originalValue = _conditions[conditionIndex];
        _conditions[conditionIndex] = value;

        // If we changed the value of one of the conditions, we check whether any of the actions need to
        // be executed
        if (originalValue != value) {
            // We only execute the corresponding action if this new value was responsible for the compound
            // condition to become its other value, so every other condition needs to be true in both cases
            for (var i = 0; i < _conditions.Length; i++) {
                if (i == conditionIndex) {
                    continue;
                }

                // If a condition was false, we return immediately
                if (!_conditions[i]) {
                    return;
                }
            }

            if (value) {
                // All other conditions were true, so this new value made the compound condition true
                _enableAction.Invoke();
            } else {
                // All other conditions were true, so this new value made the compound condition false
                _disableAction.Invoke();
            }
        }
    }
}
