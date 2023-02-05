using System;
using Hkmp.Api.Eventing;

namespace Hkmp.Eventing;

/// <summary>
/// A handle to an execution strategy combined with its subscription token.
/// </summary>
internal class EventSubscription {
    /// <summary>
    /// The token that represents the subscription and can be used to unsubscribe.
    /// </summary>
    public SubscriptionToken SubscriptionToken { get; }

    /// <summary>
    /// The strategy to execute for this particular subscription.
    /// </summary>
    public Action<object> ExecutionStrategy { get; }

    public EventSubscription(SubscriptionToken token, Action<object> strategy) {
        SubscriptionToken = token;
        ExecutionStrategy = strategy;
    }
}
