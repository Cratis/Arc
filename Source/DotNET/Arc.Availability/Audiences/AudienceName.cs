// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// Represents the name of an audience: a named group a tenant, a user or the system can belong to.
/// </summary>
/// <remarks>
/// Names are compared exactly (ordinal, case-sensitive), as the model declares them.
/// </remarks>
/// <param name="Value">The actual value.</param>
public record AudienceName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="AudienceName"/>.
    /// </summary>
    /// <param name="value">String to convert from.</param>
    public static implicit operator AudienceName(string value) => new(value);

    /// <summary>
    /// Implicitly convert from <see cref="AudienceName"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"><see cref="AudienceName"/> to convert from.</param>
    public static implicit operator string(AudienceName value) => value.Value;

    /// <summary>
    /// Gets the name of an audience declared as a type: the name of the type.
    /// </summary>
    /// <param name="audience">The type of the audience, which derives from <see cref="Audience"/>.</param>
    /// <returns>The name.</returns>
    public static AudienceName Of(Type audience) => new(audience.Name);

    /// <summary>
    /// Gets the name of an audience declared as a type: the name of the type.
    /// </summary>
    /// <typeparam name="TAudience">The type of the audience.</typeparam>
    /// <returns>The name.</returns>
    public static AudienceName Of<TAudience>()
        where TAudience : Audience => Of(typeof(TAudience));
}
