// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Arc.Commands;

/// <summary>
/// Provides read-only access to the generated values captured by a command in resolution order.
/// </summary>
public sealed class GeneratedValues : IReadOnlyList<GeneratedValue>
{
    readonly List<GeneratedValue> _values = [];

    /// <inheritdoc/>
    public int Count => _values.Count;

    /// <inheritdoc/>
    public GeneratedValue this[int index] => _values[index];

    /// <summary>
    /// Gets the most recently captured value with the given name and declared type.
    /// </summary>
    /// <typeparam name="T">The declared generated value type.</typeparam>
    /// <param name="name">The generated value name.</param>
    /// <returns>The captured value.</returns>
    public T Get<T>(string name) => (T)_values.Last(value => value.Name == name && value.Type == typeof(T)).Value;

    /// <inheritdoc/>
    public IEnumerator<GeneratedValue> GetEnumerator() => _values.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Captures a validated value at the next resolution-order index.
    /// </summary>
    /// <param name="name">The generated value name.</param>
    /// <param name="type">The declared type.</param>
    /// <param name="value">The validated value.</param>
    /// <param name="freshness">The parameter's freshness policy.</param>
    internal void Add(string name, Type type, object value, GeneratedValueFreshness freshness = GeneratedValueFreshness.Guarded) =>
        _values.Add(new(_values.Count, name, type, value) { Freshness = freshness });
}
