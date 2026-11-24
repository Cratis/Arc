// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// A 'when' that is an <see cref="IAvailabilityCondition"/> class, for anything a comparison cannot say.
/// </summary>
/// <param name="Condition">The type of the condition.</param>
public sealed record ConditionClassTerm(Type Condition) : ConditionTerm
{
    /// <inheritdoc/>
    public override IAvailabilityCondition ToCondition(Func<Type, IAvailabilityCondition> conditions) => conditions(Condition);
}
