using System;
using Hkmp.Api.Eventing;

namespace Hkmp.Eventing
{
    /// <summary>
    /// A handle to an execution strategy combined with it's subscription token. For internal use only.
    /// </summary>
    internal class EventSubscription
    {
        /// <summary>
        /// The token to unsubscribe with
        /// </summary>
        public SubscriptionToken SubscriptionToken { get; }

        /// <summary>
        /// The strategy to execute for this particular subscription
        /// </summary>
        public Action<object> ExecutionStrategy { get; }
        
        /// <summary>
        /// Ctor
        /// </summary>
        public EventSubscription(SubscriptionToken token, Action<object> strategy)
        {
            SubscriptionToken = token;
            ExecutionStrategy = strategy;
        }
    }
}
