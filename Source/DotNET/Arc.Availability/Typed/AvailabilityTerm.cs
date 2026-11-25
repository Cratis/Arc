// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// One term of the lines of an <see cref="AvailabilityBlock"/>: a 'for', 'with' or 'when' that can be combined with 'and' and 'or'.
/// </summary>
public abstract record AvailabilityTerm
{
    /// <summary>
    /// Combines two terms with 'and'.
    /// </summary>
    /// <param name="left">The first term.</param>
    /// <param name="right">The second term.</param>
    /// <returns>The term that holds when both hold.</returns>
    public static AvailabilityTerm operator &(AvailabilityTerm left, AvailabilityTerm right) => new AllOfTerm(left, right);

    /// <summary>
    /// Combines two terms with 'or'.
    /// </summary>
    /// <param name="left">The first term.</param>
    /// <param name="right">The second term.</param>
    /// <returns>The term that holds when either holds.</returns>
    public static AvailabilityTerm operator |(AvailabilityTerm left, AvailabilityTerm right) => new AnyOfTerm(left, right);

    /// <summary>
    /// Builds the expression the engine evaluates.
    /// </summary>
    /// <param name="conditions">Creates the condition of an <c>IAvailabilityCondition</c> class, resolving what it injects.</param>
    /// <returns>The expression.</returns>
    public abstract AvailabilityExpression ToExpression(Func<Type, IAvailabilityCondition> conditions);
}
