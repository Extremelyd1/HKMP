using System;

namespace Hkmp.Api.Eventing
{
    /// <summary>
    /// Subscription token for an event. Allows you to unsubscribe from an event.
    /// </summary>
    public class SubscriptionToken : IEquatable<SubscriptionToken>, IDisposable
    {
        private readonly Guid _token;
        private Action<SubscriptionToken> _unsubscribeAction;

        /// <summary>
        /// Ctor
        /// </summary>
        public SubscriptionToken(Action<SubscriptionToken> unsubscribeAction)
        {
            _unsubscribeAction = unsubscribeAction;
            _token = new Guid();
        }

        /// <summary>
        /// Equality is overriden for comparison
        /// </summary>
        public bool Equals(SubscriptionToken other)
        {
            if (other == null) return false;
            return Equals(_token, other._token);
        }

        /// <summary>
        /// Equality is overriden for comparison
        /// </summary>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            return Equals(obj as SubscriptionToken);
        }

        /// <summary>
        /// Hashcode is overriden for comparison
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }

        /// <summary>
        /// IDisposable
        /// </summary>
        public virtual void Dispose()
        {
            if (this._unsubscribeAction != null)
            {
                this._unsubscribeAction(this);
                this._unsubscribeAction = null;
            }

            GC.SuppressFinalize(this);
        }
    }
}
