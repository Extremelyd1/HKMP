using System;
using System.Collections.Generic;
using System.Linq;
using Hkmp.Eventing;

namespace Hkmp.Api.Eventing;

/// <summary>
/// Base type for all inter-mod events (e.g. from one client-side mod to another client-side mod on the same device).
/// </summary>
public class InterEvent {
    /// <summary>
    /// List containing event subscriptions for this event.
    /// </summary>
    private readonly List<EventSubscription> _subscriptions = new List<EventSubscription>();

    /// <summary>
    /// Unsubscribes an action from the event by its token.
    /// </summary>
    /// <param name="token">The token that denotes the subscription.</param>
    public virtual void Unsubscribe(SubscriptionToken token) {
        var sub = _subscriptions.FirstOrDefault(t => t.SubscriptionToken.Equals(token));

        if (sub != null) {
            _subscriptions.Remove(sub);
        }
    }

    /// <summary>
    /// Internal implementation of subscribe, with types erased.
    /// </summary>
    /// <param name="strategy">The strategy to execute when an event is triggered.</param>
    /// <returns>A token that represents this subscription and can be used to unsubscribe.</returns>
    protected internal SubscriptionToken SubscribeInternal(Action<object> strategy) {
        var sub = new EventSubscription(new SubscriptionToken(Unsubscribe), strategy);
        _subscriptions.Add(sub);
        return sub.SubscriptionToken;
    }

    /// <summary>
    /// Internal implementation of publish, with types erased.
    /// </summary>
    /// <param name="payload">The payload object to publish.</param>
    protected internal void PublishInternal(object payload) {
        foreach (var subscription in _subscriptions) {
            subscription.ExecutionStrategy(payload);
        }
    }
}
