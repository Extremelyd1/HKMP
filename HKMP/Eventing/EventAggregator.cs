using System;
using System.Collections.Generic;
using Hkmp.Api.Eventing;

namespace Hkmp.Eventing {
    /// <inheritdoc />
    internal class EventAggregator : IEventAggregator {
        /// <summary>
        /// Dictionary mapping event types to their <see cref="EventBase"/> instances.
        /// </summary>
        private readonly Dictionary<Type, EventBase> _events = new Dictionary<Type, EventBase>();

        /// <inheritdoc />
        public TEventType GetEvent<TEventType>() where TEventType : EventBase, new() {
            if (_events.TryGetValue(typeof(TEventType), out var eventBase)) {
                return (TEventType)eventBase;
            }

            // No event registered yet, so we create a new one
            var newEvent = new TEventType();
            _events[typeof(TEventType)] = newEvent;
            return newEvent;
        }
    }
}
