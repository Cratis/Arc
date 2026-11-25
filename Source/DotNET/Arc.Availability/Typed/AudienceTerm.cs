// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability;

/// <summary>
/// A 'for' term over one audience, or its negation.
/// </summary>
/// <param name="Audience">The type of the audience.</param>
/// <param name="Negated">Whether the term holds when the audience is not held. An unknown audience stays closed either way.</param>
public sealed record AudienceTerm(Type Audience, bool Negated) : AvailabilityTerm
{
    /// <inheritdoc/>
    public override AvailabilityExpression ToExpression(Func<Type, IAvailabilityCondition> conditions)
    {
        var name = AudienceName.Of(Audience);
        return Negated ? new AvailableNotAudience(name) : new AvailableForAudience(name);
    }
}
