// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authentication;

/// <summary>
/// Represents the reason for an authentication failure.
/// </summary>
/// <param name="Value">The reason value.</param>
public record AuthenticationFailureReason(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a string to an <see cref="AuthenticationFailureReason"/>.
    /// </summary>
    /// <param name="value">The string value.</param>
    public static implicit operator AuthenticationFailureReason(string value) => new(value);
}
