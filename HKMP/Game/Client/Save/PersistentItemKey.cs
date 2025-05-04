using System;

namespace Hkmp.Game.Client.Save; 

/// <summary>
/// Class to identify a persistent item by its ID and scene name.
/// </summary>
internal class PersistentItemKey : IEquatable<PersistentItemKey> {
    /// <summary>
    /// The ID of the item.
    /// </summary>
    public string Id { get; init; }
    /// <summary>
    /// The name of the scene of the item.
    /// </summary>
    public string SceneName { get; init; }

    /// <inheritdoc />
    public bool Equals(PersistentItemKey other) {
        if (ReferenceEquals(null, other)) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return Id == other.Id && SceneName == other.SceneName;
    }

    /// <inheritdoc />
    public override bool Equals(object obj) {
        if (ReferenceEquals(null, obj)) {
            return false;
        }

        if (ReferenceEquals(this, obj)) {
            return true;
        }

        if (obj.GetType() != GetType()) {
            return false;
        }

        return Equals((PersistentItemKey) obj);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            return ((Id != null ? Id.GetHashCode() : 0) * 397) ^ (SceneName != null ? SceneName.GetHashCode() : 0);
        }
    }
    
    public static bool operator ==(PersistentItemKey left, PersistentItemKey right) {
        return Equals(left, right);
    }

    public static bool operator !=(PersistentItemKey left, PersistentItemKey right) {
        return !Equals(left, right);
    }

    /// <inheritdoc />
    public override string ToString() {
        return $"({Id}, {SceneName})";
    }
}
