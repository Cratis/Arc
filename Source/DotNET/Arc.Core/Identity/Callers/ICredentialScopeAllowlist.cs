// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Describes a credential installation's immutable scope ceiling for startup diagnostics, not authorization.
/// </summary>
public interface ICredentialScopeAllowlist
{
    /// <summary>Gets the credential kind whose configured ceiling this snapshot describes.</summary>
    CallerCredentialKind Kind { get; }

    /// <summary>Gets the explicit, case-sensitive scopes configured for this credential installation.</summary>
    IReadOnlySet<string> Scopes { get; }
}
