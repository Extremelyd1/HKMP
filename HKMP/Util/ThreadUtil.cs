using System;
using System.Collections.Generic;
using UnityEngine;

namespace Hkmp.Util;

/// <summary>
/// Class for utilities regarding threading.
/// </summary>
internal class ThreadUtil : MonoBehaviour {
    /// <summary>
    /// Object to lock asynchronous access.
    /// </summary>
    private static readonly object Lock = new object();

    /// <summary>
    /// List of actions that need to be run on the Unity main thread.
    /// </summary>
    private static readonly List<Action> ActionsToRun = new List<Action>();

    /// <summary>
    /// Instantiate this static class.
    /// </summary>
    public static void Instantiate() {
        var threadUtilObject = new GameObject();
        threadUtilObject.AddComponent<ThreadUtil>();
        DontDestroyOnLoad(threadUtilObject);
    }

    /// <summary>
    /// Runs the given action on the main thread of Unity.
    /// </summary>
    /// <param name="action">The action to run.</param>
    public static void RunActionOnMainThread(Action action) {
        lock (Lock) {
            ActionsToRun.Add(action);
        }
    }

    public void Update() {
        lock (Lock) {
            foreach (var action in ActionsToRun) {
                action.Invoke();
            }

            ActionsToRun.Clear();
        }
    }
}
