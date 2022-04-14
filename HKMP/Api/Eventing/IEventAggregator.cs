using JetBrains.Annotations;

namespace Hkmp.Api.Eventing {
    /// <summary>
    /// Inter-mod event aggregation service. For sending events between mods on the same system (e.g. from one
    /// client-side mod to another client-side mod on the same device). This will not send events across the
    /// network connection.
    /// </summary>
    [PublicAPI]
    public interface IEventAggregator {
        /// <summary>
        /// Returns an event of a given type.
        /// </summary>
        /// <typeparam name="TEventType">The type of the event to return.</typeparam>
        /// <returns>An <see cref="EventBase"/> instance.</returns>
        TEventType GetEvent<TEventType>() where TEventType : EventBase, new();
    }
}