// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability;

/// <summary>
/// Two conditions joined with 'and' or 'or' that are one <c>when</c>.
/// </summary>
/// <remarks>
/// The logic is three-valued: an unknown part keeps the whole closed unless the other part decides the result alone.
/// </remarks>
/// <param name="all">Whether both must hold ('and'), or either may ('or').</param>
/// <param name="left">The first condition.</param>
/// <param name="right">The second condition.</param>
public class CompositeCondition(bool all, IAvailabilityCondition left, IAvailabilityCondition right) : IAvailabilityCondition
{
    /// <inheritdoc/>
    public string Description => $"({left.Description}) {(all ? "and" : "or")} ({right.Description})";

    /// <inheritdoc/>
    public async ValueTask<AvailabilityOutcome> Evaluate(IContextValueResolver context, CancellationToken cancellationToken)
    {
        var first = await left.Evaluate(context, cancellationToken);
        var decides = all ? AvailabilityOutcome.Unavailable : AvailabilityOutcome.Available;
        if (first == decides)
        {
            return first;
        }

        var second = await right.Evaluate(context, cancellationToken);
        if (second == decides)
        {
            return second;
        }

        return first == AvailabilityOutcome.Unknown || second == AvailabilityOutcome.Unknown
            ? AvailabilityOutcome.Unknown
            : all ? AvailabilityOutcome.Available : AvailabilityOutcome.Unavailable;
    }
}
