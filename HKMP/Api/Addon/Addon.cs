namespace Hkmp.Api.Addon;

/// <summary>
/// Abstract base class for addons.
/// </summary>
public abstract class Addon {
    /// <summary>
    /// The maximum length of the name string for an addon.
    /// </summary>
    public const int MaxNameLength = 20;

    /// <summary>
    /// The maximum length of the version string for an addon.
    /// </summary>
    public const int MaxVersionLength = 10;

    /// <summary>
    /// The internal ID assigned to this addon.
    /// </summary>
    internal byte? Id { get; set; }

    /// <summary>
    /// The network sender object if it has been registered.
    /// </summary>
    internal object NetworkSender;

    /// <summary>
    /// The network receiver object if it has been registered.
    /// </summary>
    internal object NetworkReceiver;
}
