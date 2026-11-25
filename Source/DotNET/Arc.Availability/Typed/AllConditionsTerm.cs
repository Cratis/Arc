// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Two conditions that must both hold, as one 'when'.
/// </summary>
/// <param name="Left">The first condition.</param>
/// <param name="Right">The second condition.</param>
public sealed record AllConditionsTerm(ConditionTerm Left, ConditionTerm Right) : ConditionTerm
{
    /// <inheritdoc/>
    public override IAvailabilityCondition ToCondition(Func<Type, IAvailabilityCondition> conditions) =>
        new CompositeCondition(true, Left.ToCondition(conditions), Right.ToCondition(conditions));
}
