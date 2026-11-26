// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability;

/// <summary>
/// A condition with a fixed outcome that counts how often it was evaluated, or fails.
/// </summary>
/// <param name="outcome">The outcome to answer.</param>
/// <param name="description">The description.</param>
/// <param name="fails">Whether evaluating throws.</param>
public class FixedCondition(AvailabilityOutcome outcome, string description = "cond", bool fails = false) : IAvailabilityCondition
{
    public int Evaluations { get; private set; }

    public string Description => description;

    public ValueTask<AvailabilityOutcome> Evaluate(IContextValueResolver context, CancellationToken cancellationToken)
    {
        Evaluations++;
        return fails ? throw new InvalidOperationException("store down") : ValueTask.FromResult(outcome);
    }
}
