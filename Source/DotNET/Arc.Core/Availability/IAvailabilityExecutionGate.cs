// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// An optional availability gate capability that decides from the bound invocation, not just its operation type.
/// </summary>
public interface IAvailabilityExecutionGate : IAvailabilityGate
{
    /// <summary>
    /// Decides whether this invocation is available in its own service scope.
    /// </summary>
    /// <param name="invocation">The bound invocation, including the server's operation receipt.</param>
    /// <param name="services">The services scoped to this invocation.</param>
    /// <param name="cancellationToken">The invocation's cancellation token.</param>
    /// <returns>The availability decision. Unknown never permits execution.</returns>
    /// <remarks>
    /// Provider failures propagate for the pipeline to report as retryable errors. Implementing this capability does not
    /// change the metadata-only overload of <see cref="IAvailabilityGate.Decide"/> or replace authorization.
    /// </remarks>
    ValueTask<AvailabilityDecision> Decide(AvailabilityInvocation invocation, IServiceProvider services, CancellationToken cancellationToken);
}
