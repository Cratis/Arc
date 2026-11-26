// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Two terms of which either must hold.
/// </summary>
/// <param name="Left">The first term.</param>
/// <param name="Right">The second term.</param>
public sealed record AnyOfTerm(AvailabilityTerm Left, AvailabilityTerm Right) : AvailabilityTerm
{
    /// <inheritdoc/>
    public override AvailabilityExpression ToExpression(Func<Type, IAvailabilityCondition> conditions) =>
        new AvailabilityOr([Left.ToExpression(conditions), Right.ToExpression(conditions)]);
}
