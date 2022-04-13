namespace Hkmp.Api.Eventing
{
    /// <summary>
    /// Inter-mod event aggregation service. For sending events between mods on the same system (eg Client mod to client mod on player 1s computer)
    /// This will not send events across the network connections.
    /// </summary>
    public interface IEventAggregator
    {
        /// <summary>
        /// Returns an event of a given type
        /// </summary>
        /// <typeparam name="TEventType">The type of the event to return</typeparam>
        /// <returns><see cref="EventBase"/></returns>
        TEventType GetEvent<TEventType>() where TEventType : EventBase, new();
    }
}
