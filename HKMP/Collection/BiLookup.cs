using System;
using System.Collections;
using System.Collections.Generic;

namespace Hkmp.Collection;

/// <summary>
/// Bi-directional lookup table.
/// </summary>
/// <typeparam name="TFirst">The first type.</typeparam>
/// <typeparam name="TSecond">The second type.</typeparam>
public class BiLookup<TFirst, TSecond> : IEnumerable<KeyValuePair<TFirst, TSecond>> {
    /// <summary>
    /// Dictionary containing the mapping from first type to second type.
    /// </summary>
    private readonly Dictionary<TFirst, TSecond> _normal;

    /// <summary>
    /// Dictionary containing the mapping from second type to first type.
    /// </summary>
    private readonly Dictionary<TSecond, TFirst> _inverse;

    /// <summary>
    /// Constructs the bi-directional lookup table.
    /// </summary>
    public BiLookup() {
        _normal = new Dictionary<TFirst, TSecond>();
        _inverse = new Dictionary<TSecond, TFirst>();
    }

    /// <summary>
    /// The number of elements in this lookup table.
    /// </summary>
    public int Count => _normal.Count;

    /// <summary>
    /// Add an entry with the given values to the lookup table.
    /// </summary>
    /// <param name="first">The first value of the entry to add.</param>
    /// <param name="second">The second value of the entry to add.</param>
    /// <exception cref="ArgumentException">Thrown if the either part of the entry is already present in the
    /// lookup table.</exception>
    public void Add(TFirst first, TSecond second) {
        if (_normal.ContainsKey(first)) {
            throw new ArgumentException("Duplicate key in normal direction");
        }

        if (_inverse.ContainsKey(second)) {
            throw new ArgumentException("Duplication key in inverse direction");
        }

        _normal.Add(first, second);
        _inverse.Add(second, first);
    }

    /// <inheritdoc cref="GetByFirst"/>
    public TSecond this[TFirst index] => GetByFirst(index);

    /// <inheritdoc cref="GetBySecond"/>
    public TFirst this[TSecond index] => GetBySecond(index);

    /// <summary>
    /// Get the associated value with the given index.
    /// </summary>
    /// <param name="index">The index with type as the first type of this lookup.</param>
    /// <returns>The associated value as second type or default for the given type.</returns>
    public TSecond GetByFirst(TFirst index) {
        if (_normal.TryGetValue(index, out var value)) {
            return value;
        }

        return default;
    }

    /// <summary>
    /// Get the associated value with the given index.
    /// </summary>
    /// <param name="index">The index with type as the second type of this lookup.</param>
    /// <returns>The associated value as first type or default for the given type.</returns>
    public TFirst GetBySecond(TSecond index) {
        if (_inverse.TryGetValue(index, out var value)) {
            return value;
        }

        return default;
    }

    /// <summary>
    /// Try to get the value corresponding to the given index.
    /// </summary>
    /// <param name="index">The index to find the value for.</param>
    /// <param name="value">Will contain the value for the given index if found. Default otherwise.</param>
    /// <returns>True if the value for the index was found, false otherwise.</returns>
    public bool TryGetValue(TFirst index, out TSecond value) {
        if (!ContainsFirst(index)) {
            value = default;
            return false;
        }

        value = GetByFirst(index);
        return true;
    }

    /// <summary>
    /// Try to get the value corresponding to the given index.
    /// </summary>
    /// <param name="index">The index to find the value for.</param>
    /// <param name="value">Will contain the value for the given index if found. Default otherwise.</param>
    /// <returns>True if the value for the index was found, false otherwise.</returns>
    public bool TryGetValue(TSecond index, out TFirst value) {
        if (!ContainsSecond(index)) {
            value = default;
            return false;
        }

        value = GetBySecond(index);
        return true;
    }

    /// <summary>
    /// Whether the given value exists in this lookup.
    /// </summary>
    /// <param name="index">The index with type as the first type of this lookup.</param>
    /// <returns>True if the value exists in the lookup, false otherwise.</returns>
    public bool ContainsFirst(TFirst index) {
        return _normal.ContainsKey(index);
    }

    /// <summary>
    /// Whether the given value exists in this lookup.
    /// </summary>
    /// <param name="index">The index with type as the second type of this lookup.</param>
    /// <returns>True if the value exists in the lookup, false otherwise.</returns>
    public bool ContainsSecond(TSecond index) {
        return _inverse.ContainsKey(index);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<TFirst, TSecond>> GetEnumerator() {
        return _normal.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}
