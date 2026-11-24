// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability;

/// <summary>
/// Defines a condition over the request context: the body of a 'when' line.
/// </summary>
public interface IAvailabilityCondition
{
    /// <summary>
    /// Gets the condition as authored, used to explain why something is not available.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Evaluates the condition.
    /// </summary>
    /// <param name="context">The context of the request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="AvailabilityOutcome.Unknown"/> when a value the condition needs is absent. Throw when reading a value failed.
    /// </returns>
    ValueTask<AvailabilityOutcome> Evaluate(IContextValueResolver context, CancellationToken cancellationToken);
}
