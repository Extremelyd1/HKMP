using System.Collections.Generic;
using UnityEngine;

namespace Hkmp.Util;

/// <summary>
/// Class for GameObject utility methods and extensions.
/// </summary>
internal static class GameObjectUtil {
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

    /// <summary>
    /// Get a list of the children of the given GameObject.
    /// </summary>
    /// <param name="gameObject">The GameObject to get the children for.</param>
    /// <returns>A list of the children of the GameObject.</returns>
    public static List<GameObject> GetChildren(this GameObject gameObject) {
        var children = new List<GameObject>();
        for (var i = 0; i < gameObject.transform.childCount; i++) {
            children.Add(gameObject.transform.GetChild(i).gameObject);
        }

        return children;
    }
    
    /// <summary>
    /// Find an inactive GameObject with the given name.
    /// </summary>
    /// <param name="name">The name of the GameObject.</param>
    /// <returns>The GameObject is it exists, null otherwise.</returns>
    public static GameObject FindInactiveGameObject(string name) {
        var transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var transform in transforms) {
            if (transform.hideFlags == HideFlags.None) {
                if (transform.name == name) {
                    return transform.gameObject;
                }
            }
        }

        return null;
    }
}
