// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>A terminal independent OAuth verification result.</summary>
public sealed class OAuthBearerVerification
{
    OAuthBearerVerification(OAuthBearerVerified? value) => Value = value;

    /// <summary>Gets complete verified evidence, or null for refusal.</summary>
    public OAuthBearerVerified? Value { get; }

    /// <summary>Returns independently verified token evidence.</summary>
    /// <param name="value">The complete verified evidence.</param>
    /// <returns>The verified result.</returns>
    public static OAuthBearerVerification Verified(OAuthBearerVerified value) => new(value);

    /// <summary>Refuses this token without trying another credential source.</summary>
    /// <returns>A terminal refusal.</returns>
    public static OAuthBearerVerification Refused() => new(null);
}
