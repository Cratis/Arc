// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// A 'when' term: one condition, with its own 'and' and 'or' that never mix with the 'for' and 'with' of a line.
/// </summary>
public abstract record ConditionTerm : AvailabilityTerm
{
    /// <summary>
    /// Combines two conditions with 'and' into one condition.
    /// </summary>
    /// <param name="left">The first condition.</param>
    /// <param name="right">The second condition.</param>
    /// <returns>The condition that holds when both hold.</returns>
    public static ConditionTerm operator &(ConditionTerm left, ConditionTerm right) => new AllConditionsTerm(left, right);

    /// <summary>
    /// Combines two conditions with 'or' into one condition.
    /// </summary>
    /// <param name="left">The first condition.</param>
    /// <param name="right">The second condition.</param>
    /// <returns>The condition that holds when either holds.</returns>
    public static ConditionTerm operator |(ConditionTerm left, ConditionTerm right) => new AnyConditionTerm(left, right);

    /// <inheritdoc/>
    public override AvailabilityExpression ToExpression(Func<Type, IAvailabilityCondition> conditions) =>
        new AvailableWhen(ToCondition(conditions));

    /// <summary>
    /// Builds the condition the engine evaluates.
    /// </summary>
    /// <param name="conditions">Creates the condition of an <c>IAvailabilityCondition</c> class.</param>
    /// <returns>The condition.</returns>
    public abstract IAvailabilityCondition ToCondition(Func<Type, IAvailabilityCondition> conditions);
}
