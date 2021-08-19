using System.Collections.Generic;

namespace Hkmp.Ui {
    public class ComponentGroup {
        private readonly List<ComponentGroup> _children;
        private readonly List<Component.IComponent> _components;

        private ComponentGroup _parent;
        private bool _activeSelf;

        public ComponentGroup(bool activeSelf = true, ComponentGroup parent = null) {
            _children = new List<ComponentGroup>();
            _components = new List<Component.IComponent>();

            _activeSelf = activeSelf;

            SetParent(parent);
        }

        /**
         * Returns whether the parent hierarchy is active
         */
        private bool IsHierarchyActive() {
            return _activeSelf && (_parent == null || _parent.IsHierarchyActive());
        }

        /**
         * If the parent or its hierarchy changes, this method is called
         */
        private void OnParentUpdate(bool hierarchyActive) {
            // Check whether we need to activate or deactivate our own components
            var newActive = hierarchyActive && _activeSelf;

            SetComponentsActive(newActive);

            // Propagate this to all children
            foreach (var child in _children) {
                child.OnParentUpdate(newActive);
            }
        }

        private void SetComponentsActive(bool active) {
            foreach (var component in _components) {
                component?.SetGroupActive(active);
            }
        }

        public void SetParent(ComponentGroup parent) {
            _parent = parent;

            parent?._children.Add(this);

            // The parent changed, so we need to check whether our components or children should
            // still be activated
            OnParentUpdate(IsHierarchyActive());
        }

        public void AddComponent(Component.IComponent component) {
            _components.Add(component);

            component.SetGroupActive(_activeSelf && IsHierarchyActive());
        }

        public void SetActive(bool active) {
            _activeSelf = active;

            // Check whether the parent hierarchy is active and call the update
            OnParentUpdate(IsHierarchyActive());
        }

        public bool IsActive() {
            return _activeSelf && IsHierarchyActive();
        }
    }
}