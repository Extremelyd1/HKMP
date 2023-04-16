using System;

namespace Hkmp.Api.Eventing;

/// <summary>
/// Subscription token for an event. Allows you to unsubscribe from an event.
/// </summary>
public class SubscriptionToken : IEquatable<SubscriptionToken>, IDisposable {
    /// <summary>
    /// Unique identifier for this token.
    /// </summary>
    private readonly Guid _guid;

    /// <summary>
    /// The action that should be executed on unsubscribe.
    /// </summary>
    private Action<SubscriptionToken> _unsubscribeAction;

    /// <summary>
    /// Constructor for the subscription token with a given unsubscribe action.
    /// </summary>
    /// <param name="unsubscribeAction">The action that should be executed on unsubscribe.</param>
    public SubscriptionToken(Action<SubscriptionToken> unsubscribeAction) {
        _unsubscribeAction = unsubscribeAction;
        _guid = new Guid();
    }

    /// <inheritdoc />
    public bool Equals(SubscriptionToken other) {
        if (other == null) {
            return false;
        }

        return Equals(_guid, other._guid);
    }

    /// <inheritdoc />
    public override bool Equals(object obj) {
        return ReferenceEquals(this, obj) || Equals(obj as SubscriptionToken);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return _guid.GetHashCode();
    }

    /// <inheritdoc />
    public virtual void Dispose() {
        if (_unsubscribeAction != null) {
            _unsubscribeAction(this);
            _unsubscribeAction = null;
        }

        GC.SuppressFinalize(this);
    }
}
