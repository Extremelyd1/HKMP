using System;
using System.Collections.Generic;
using Hkmp.Api.Eventing;

namespace Hkmp.Eventing;

/// <inheritdoc />
internal class EventAggregator : IEventAggregator {
    /// <summary>
    /// Dictionary mapping event types to their <see cref="InterEvent"/> instances.
    /// </summary>
    private readonly Dictionary<Type, InterEvent> _events = new Dictionary<Type, InterEvent>();

    /// <inheritdoc />
    public TEventType GetEvent<TEventType>() where TEventType : InterEvent, new() {
        if (_events.TryGetValue(typeof(TEventType), out var eventBase)) {
            return (TEventType) eventBase;
        }

        // No event registered yet, so we create a new one
        var newEvent = new TEventType();
        _events[typeof(TEventType)] = newEvent;
        return newEvent;
    }
}
