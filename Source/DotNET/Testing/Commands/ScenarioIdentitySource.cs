// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Supplies queued identities before falling back to newly generated ones.
/// </summary>
internal class ScenarioIdentitySource : IIdentitySource
{
    readonly ConcurrentQueue<Guid> _values = new();
    readonly IdentitySource _fallback = new();

    /// <inheritdoc/>
    public Guid NewGuid() => _values.TryDequeue(out var value) ? value : _fallback.NewGuid();

    /// <summary>
    /// Appends identities in their consumption order.
    /// </summary>
    /// <param name="values">The identities to append.</param>
    public void Enqueue(IEnumerable<Guid> values)
    {
        foreach (var value in values)
        {
            _values.Enqueue(value);
        }
    }
}
