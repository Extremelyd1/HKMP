using System;

namespace Hkmp.Api.Eventing;

/// <summary>
/// A publish-subscribe style event built off <see cref="InterEvent"/> with a typed payload.
/// </summary>
/// <typeparam name="TPayload">The type of the payload.</typeparam>
public class PubSubEvent<TPayload> : InterEvent {
    /// <summary>
    /// Subscribes to the event.
    /// </summary>
    /// <param name="action">The action to execute when the event triggers.</param>
    /// <returns>A token that represents this subscription and can be used to unsubscribe.</returns>
    public SubscriptionToken Subscribe(Action<TPayload> action) {
        // The typing keeps this safe, but I really wish we had a concept of referencing 
        // templates without providing payloads so we could avoid this cast.
        return SubscribeInternal(o => action((TPayload) o));
    }

    /// <summary>
    /// Publishes a payload to the subscribers of the event type.
    /// </summary>
    /// <param name="payload">The payload to publish.</param>
    public void Publish(TPayload payload) {
        PublishInternal(payload);
    }
}
