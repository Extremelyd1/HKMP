using System.Collections.Generic;
using Hkmp.Ui.Component;

namespace Hkmp.Ui;

/// <summary>
/// A group of components that can be enabled/disabled to manage hierarchies.
/// </summary>
internal class ComponentGroup {
    /// <summary>
    /// List of component groups as children.
    /// </summary>
    private readonly List<ComponentGroup> _children;

    /// <summary>
    /// List of components as children.
    /// </summary>
    private readonly List<IComponent> _components;

    /// <summary>
    /// The parent of this group.
    /// </summary>
    private ComponentGroup _parent;

    /// <summary>
    /// Whether this group is active.
    /// </summary>
    private bool _activeSelf;

    public ComponentGroup(bool activeSelf = true, ComponentGroup parent = null) {
        _children = new List<ComponentGroup>();
        _components = new List<IComponent>();

        _activeSelf = activeSelf;

        SetParent(parent);
    }

    /// <summary>
    /// Returns whether the parent hierarchy is active.
    /// </summary>
    /// <returns>true if the parent hierarchy is active; otherwise false.</returns>
    private bool IsHierarchyActive() {
        return _activeSelf && (_parent == null || _parent.IsHierarchyActive());
    }

    /// <summary>
    /// Callback method for when the parent or its hierarchy changes.
    /// </summary>
    /// <param name="hierarchyActive">Whether the hierarchy is now active or not.</param>
    private void OnParentUpdate(bool hierarchyActive) {
        // Check whether we need to activate or deactivate our own components
        var newActive = hierarchyActive && _activeSelf;

        SetComponentsActive(newActive);

        // Propagate this to all children
        foreach (var child in _children) {
            child.OnParentUpdate(newActive);
        }
    }

    /// <summary>
    /// Set whether the children component should be active.
    /// </summary>
    /// <param name="active">Whether the children should be active.</param>
    private void SetComponentsActive(bool active) {
        foreach (var component in _components) {
            component?.SetGroupActive(active);
        }
    }

    /// <summary>
    /// Set the parent of this component group.
    /// </summary>
    /// <param name="parent">The new parent of this group.</param>
    public void SetParent(ComponentGroup parent) {
        _parent = parent;

        parent?._children.Add(this);

        // The parent changed, so we need to check whether our components or children should
        // still be activated
        OnParentUpdate(IsHierarchyActive());
    }

    /// <summary>
    /// Adds a component to the group.
    /// </summary>
    /// <param name="component">The component to add.</param>
    public void AddComponent(IComponent component) {
        _components.Add(component);

        component.SetGroupActive(_activeSelf && IsHierarchyActive());
    }

    /// <summary>
    /// Set whether this component group is active.
    /// </summary>
    /// <param name="active">Whether the group is active.</param>
    public void SetActive(bool active) {
        _activeSelf = active;

        // Check whether the parent hierarchy is active and call the update
        OnParentUpdate(IsHierarchyActive());
    }

    /// <summary>
    /// Whether the group is active.
    /// </summary>
    /// <returns>true if the parent hierarchy is active and the group itself is active; otherwise false.</returns>
    public bool IsActive() {
        return _activeSelf && IsHierarchyActive();
    }
}
