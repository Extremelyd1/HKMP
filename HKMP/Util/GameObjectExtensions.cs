using UnityEngine;

namespace HKMP.Util {
    public static class GameObjectExtensions {
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
}