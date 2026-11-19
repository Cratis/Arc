// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

/// <summary>
/// An availability gate whose decisions the specs set.
/// </summary>
public class AvailabilityGateForSpecs : IAvailabilityGate
{
    /// <summary>
    /// Gets the parts that are gated, with their decision.
    /// </summary>
    public Dictionary<Type, AvailabilityVerdict> Parts { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether deciding fails.
    /// </summary>
    public bool Fail { get; set; }

    /// <inheritdoc/>
    public IReadOnlyCollection<Type> GatedParts => Parts.Keys;

    /// <inheritdoc/>
    public bool IsGated(Type part) => Parts.ContainsKey(part);

    /// <inheritdoc/>
    public ValueTask<AvailabilityDecision> Decide(Type part, IServiceProvider services, CancellationToken cancellationToken) =>
        Fail
            ? throw new InvalidOperationException("store is down")
            : ValueTask.FromResult(new AvailabilityDecision(Parts[part], []));

    /// <inheritdoc/>
    public IDisposable OnChanged(Func<ValueTask> handler) => new Subscription();

    sealed class Subscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
