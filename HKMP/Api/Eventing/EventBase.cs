using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Eventing;

namespace Hkmp.Api.Eventing
{
    /// <summary>
    /// Base type for all events
    /// </summary>
    public class EventBase
    {
        private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();

        /// <summary>
        /// Unsubscribes an action from the event by it's token
        /// </summary>
        /// <param name="token">the token to unsubscribe</param>
        public virtual void Unsubscribe(SubscriptionToken token)
        {
            var sub = _subscriptions.FirstOrDefault();
            if (sub != null)
            {
                _subscriptions.Remove(sub);
            }
        }

        /// <summary>
        /// Internal implementation of subscribe, with types erased.
        /// </summary>
        /// <param name="strategy">The strategy to execute when an event is triggered</param>
        /// <returns>a token to use for unsubscription</returns>
        protected internal SubscriptionToken SubscribeInternal(Action<object> strategy)
        {
            var sub = new EventSubscription(new SubscriptionToken(Unsubscribe), strategy);
            _subscriptions.Add(sub);
            return sub.SubscriptionToken;
        }

        /// <summary>
        /// Internal implemention of publish, with types erased.
        /// </summary>
        /// <param name="payload">The payload object ot publish</param>
        protected internal void PublishInternal(object payload)
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.ExecutionStrategy(payload);
            }
        }
    }
}
