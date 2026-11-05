// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>Exactly one of not applicable, refused, or verified facts.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal sealed class CredentialVerification
{
    CredentialVerification(bool applicable, VerifiedCredentialFacts? facts, CallerRefusal? refusal)
    {
        Applicable = applicable;
        Facts = facts;
        Refusal = refusal;
    }

    /// <summary>Gets a value indicating whether the source claims this credential.</summary>
    internal bool Applicable { get; }

    /// <summary>Gets verified facts on success.</summary>
    internal VerifiedCredentialFacts? Facts { get; }

    /// <summary>Gets the terminal source refusal.</summary>
    internal CallerRefusal? Refusal { get; }

    /// <summary>Declines ownership of this credential shape.</summary>
    /// <returns>A non-claiming result.</returns>
    internal static CredentialVerification NotApplicable() => new(false, null, null);

    /// <summary>Claims and refuses the credential.</summary>
    /// <param name="reason">The refusal.</param>
    /// <returns>A terminal refusal.</returns>
    internal static CredentialVerification Refused(CallerRefusal reason) => new(true, null, reason);

    /// <summary>Claims the credential with verified facts.</summary>
    /// <param name="facts">The verified facts.</param>
    /// <returns>The checked facts.</returns>
    internal static CredentialVerification Verified(VerifiedCredentialFacts facts) => new(true, facts, null);
}
