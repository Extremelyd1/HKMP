using System;
using System.Collections.Generic;
using Hkmp.Api.Eventing;

namespace Hkmp.Eventing
{
    internal class EventAggregator: IEventAggregator
    {
        private readonly Dictionary<Type, EventBase> _events = new Dictionary<Type, EventBase>();

        public T GetEvent<T>() where T : EventBase, new()
        {
            if (_events.TryGetValue(typeof(T), out var eventBase))
            {
                return (T)eventBase;
            }

            // No event, need to make a new one.
            var newEvent = new T();
            _events[typeof(T)] = newEvent;
            return newEvent;
        }
    }
}
