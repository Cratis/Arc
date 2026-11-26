// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability.Context.Providers;

/// <summary>
/// An in-memory <see cref="IHostContextValues"/>.
/// </summary>
[IgnoreConvention]
public class InMemoryHostContextValues : IHostContextValues
{
    readonly ConcurrentDictionary<string, object> _values = new();

    /// <summary>
    /// Sets a value the host supplies.
    /// </summary>
    /// <param name="name">The name of the value.</param>
    /// <param name="value">The value.</param>
    public void Set(string name, object value) => _values[name] = value;

    /// <inheritdoc/>
    public ValueTask<ContextValueOutcome> Get(string name, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_values.TryGetValue(name, out var value) ? ContextValueOutcome.Present(value) : ContextValueOutcome.Absent);
}
