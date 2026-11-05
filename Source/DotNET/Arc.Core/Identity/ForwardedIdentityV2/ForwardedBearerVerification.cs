// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>An independent verifier result retaining the original facts.</summary>
internal sealed class ForwardedBearerVerification
{
    ForwardedBearerVerification(ForwardedBearerVerifierKind? kind, CallerAccountKey? accountKey, VerifiedCredentialFacts? facts)
    {
        Kind = kind;
        AccountKey = accountKey;
        Facts = facts;
    }

    /// <summary>Gets the independent verifier kind.</summary>
    internal ForwardedBearerVerifierKind? Kind { get; }

    /// <summary>Gets the independently verified account.</summary>
    internal CallerAccountKey? AccountKey { get; }

    /// <summary>Gets the original bearer facts.</summary>
    internal VerifiedCredentialFacts? Facts { get; }

    /// <summary>Gets whether the verifier supplied complete facts.</summary>
    internal bool IsAccepted => Kind is not null && AccountKey is not null && Facts is not null;

    /// <summary>Creates an accepted independent verification result.</summary>
    /// <param name="kind">The verifier kind.</param>
    /// <param name="accountKey">The verified account key.</param>
    /// <param name="facts">The original verified facts.</param>
    /// <returns>The accepted result.</returns>
    internal static ForwardedBearerVerification Accepted(ForwardedBearerVerifierKind kind, CallerAccountKey accountKey, VerifiedCredentialFacts facts) => new(kind, accountKey, facts);

    /// <summary>Creates a refused result with no facts.</summary>
    /// <returns>The refused result.</returns>
    internal static ForwardedBearerVerification Refused() => new(null, null, null);
}
