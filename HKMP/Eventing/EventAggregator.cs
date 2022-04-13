using System;
using System.Collections.Generic;
using Hkmp.Api.Eventing;

namespace Hkmp.Eventing
{
    internal class EventAggregator: IEventAggregator
    {
        private readonly Dictionary<Type, EventBase> _events = new Dictionary<Type, EventBase>();

        public TEventType GetEvent<TEventType>() where TEventType : EventBase, new()
        {
            if (_events.TryGetValue(typeof(TEventType), out var eventBase))
            {
                return (TEventType)eventBase;
            }

            // No event, need to make a new one.
            var newEvent = new TEventType();
            _events[typeof(TEventType)] = newEvent;
            return newEvent;
        }
    }
}
