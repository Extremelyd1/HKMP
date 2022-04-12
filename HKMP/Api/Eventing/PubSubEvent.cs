using System;

namespace Hkmp.Api.Eventing
{
    /// <summary>
    /// A pub sub style event built off eventbase with a typed payload
    /// </summary>
    /// <typeparam name="T">The type of the payload</typeparam>
    public class PubSubEvent<T>: EventBase
    {
        /// <summary>
        /// Subscribes to the event
        /// </summary>
        /// <param name="action">The action to execute when the event triggers</param>
        /// <returns>A token for use in unsubscribing</returns>
        public SubscriptionToken Subscribe(Action<T> action)
        {
            // The typing keeps this safe, but I really wish we had a concept of referencing 
            // templates without providing payloads so we could avoid this cast.
            return SubscribeInternal(o => action((T)o));
        }

        /// <summary>
        /// Publishes a payload to the event type
        /// </summary>
        /// <param name="payload">The payload to publish</param>
        public void Publish(T payload)
        {
            PublishInternal(payload);
        }
    }
}
