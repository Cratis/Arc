// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Provides validated generated value captures across a scenario's executions, including nested commands.
/// </summary>
public sealed class ScenarioGeneratedValues
{
    readonly List<GeneratedValue> _values = [];

    /// <summary>
    /// Gets the latest capture with the given name and declared type.
    /// </summary>
    /// <typeparam name="T">The declared generated value type.</typeparam>
    /// <param name="name">The case-sensitive generated value name.</param>
    /// <returns>The latest validated value, including a pinned value.</returns>
    /// <exception cref="InvalidOperationException">No value with this name and declared type has been captured.</exception>
    public T Last<T>(string name) => (T)_values.Last(value => value.Name == name && value.Type == typeof(T)).Value;

    /// <summary>
    /// Gets a read-only snapshot of matching captures in resolution order across executions.
    /// </summary>
    /// <typeparam name="T">The declared generated value type.</typeparam>
    /// <param name="name">The case-sensitive generated value name.</param>
    /// <returns>The matching validated values, or an empty list if none have been captured.</returns>
    public IReadOnlyList<T> All<T>(string name) => _values
        .Where(value => value.Name == name && value.Type == typeof(T))
        .Select(value => (T)value.Value)
        .ToList()
        .AsReadOnly();

    /// <summary>
    /// Records a validated value at the next scenario-wide resolution position.
    /// </summary>
    /// <param name="value">The validated capture.</param>
    internal void Add(GeneratedValue value) => _values.Add(value);
}
