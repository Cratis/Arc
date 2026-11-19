// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Defines the availability decisions the command and query pipelines, the client query and MCP tool listing consult.
/// </summary>
/// <remarks>
/// Arc hosts the hook only. The decision comes from an implementation such as the one in the
/// <c>Cratis.Arc.Availability</c> package, built on the Choreography availability engine. A host that registers no
/// gate pays nothing: every part is then available and no gate is consulted.
/// </remarks>
public interface IAvailabilityGate
{
    /// <summary>
    /// Gets the parts that carry an availability gate.
    /// </summary>
    IReadOnlyCollection<Type> GatedParts { get; }

    /// <summary>
    /// Determines whether a part carries an availability gate.
    /// </summary>
    /// <param name="part">The command type, or the type declaring the query.</param>
    /// <returns>True when the part is gated.</returns>
    bool IsGated(Type part);

    /// <summary>
    /// Decides whether a part is available to the context of the request.
    /// </summary>
    /// <param name="part">The command type, or the type declaring the query.</param>
    /// <param name="services">The services of the request.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="AvailabilityDecision"/>; a part that carries no gate is available.</returns>
    /// <remarks>
    /// A failure to resolve what the decision needs throws. It is never reported as an unavailable decision.
    /// </remarks>
    ValueTask<AvailabilityDecision> Decide(Type part, IServiceProvider services, CancellationToken cancellationToken);

    /// <summary>
    /// Subscribes to changes in the state the decisions are made from.
    /// </summary>
    /// <param name="handler">Called when any decision may have changed.</param>
    /// <returns>A disposable that ends the subscription.</returns>
    IDisposable OnChanged(Func<ValueTask> handler);
}
