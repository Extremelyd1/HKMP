using System;

namespace Hkmp.Api.Client;

/// <summary>
/// Abstract class for a client addon that can be toggled. Extends <see cref="ClientAddon"/>.
/// </summary>
public abstract class TogglableClientAddon : ClientAddon {
    /// <summary>
    /// Whether this addon is disabled, meaning network is restricted
    /// </summary>
    private bool _disabled;

    /// <inheritdoc cref="_disabled" />
    public bool Disabled {
        get => _disabled;
        internal set {
            var valueChanged = _disabled != value;

            _disabled = value;

            if (!valueChanged) {
                return;
            }

            if (value) {
                try {
                    OnDisable();
                } catch (Exception e) {
                    Logger.Error($"Exception was thrown while calling OnDisable for addon '{GetName()}':\n{e}");
                }
            } else {
                try {
                    OnEnable();
                } catch (Exception e) {
                    Logger.Error($"Exception was thrown while calling OnEnable for addon '{GetName()}':\n{e}");
                }
            }
        }
    }

    /// <summary>
    /// Callback method for when this addon gets enabled.
    /// </summary>
    protected abstract void OnEnable();

    /// <summary>
    /// Callback method for when this addon gets disabled.
    /// </summary>
    protected abstract void OnDisable();
}
