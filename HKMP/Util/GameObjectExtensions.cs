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
}
