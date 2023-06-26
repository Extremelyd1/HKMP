using System.Collections.Generic;
using UnityEngine;

namespace Hkmp.Util;

/// <summary>
/// Class for GameObject extensions.
/// </summary>
internal static class GameObjectExtensions {
    /// <summary>
    /// Find a GameObject with the given name in the children of the given GameObject.
    /// </summary>
    /// <param name="gameObject">The GameObject to search in.</param>
    /// <param name="name">The name of the GameObject to search for.</param>
    /// <returns>The GameObject if found, null otherwise.</returns>
    public static GameObject FindGameObjectInChildren(
        this GameObject gameObject,
        string name
    ) {
        if (gameObject == null) {
            return null;
        }

        foreach (var componentsInChild in gameObject.GetComponentsInChildren<Transform>(true)) {
            if (componentsInChild.name == name) {
                return componentsInChild.gameObject;
            }
        }

        return null;
    }

    public static List<GameObject> GetChildren(this GameObject gameObject) {
        var children = new List<GameObject>();
        for (var i = 0; i < gameObject.transform.childCount; i++) {
            children.Add(gameObject.transform.GetChild(i).gameObject);
        }

        return children;
    }
}
