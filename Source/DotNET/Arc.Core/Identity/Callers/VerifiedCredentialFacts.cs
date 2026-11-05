// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Facts supplied by a credential verifier after cryptographic and binding checks, not untrusted request claims.
/// A grant must come from the credential or a server-side credential-id mapping. Proof outcomes must include replay checking.
/// </summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal sealed record VerifiedCredentialFacts
{
    /// <summary>Initializes verified credential facts; never call from a request-claim mapper.</summary>
    /// <param name="principalKind">The verified principal kind.</param>
    /// <param name="credentialKind">The verified credential kind.</param>
    /// <param name="credentialId">The credential identifier.</param>
    /// <param name="target">The credential's verified resource, audience, and environment, independent of installation configuration.</param>
    internal VerifiedCredentialFacts(CallerPrincipalKind principalKind, CallerCredentialKind credentialKind, CredentialId credentialId, CallerCredentialTarget target)
    {
        PrincipalKind = principalKind;
        CredentialKind = credentialKind;
        CredentialId = credentialId;
        Resource = target.Resource;
        Audience = target.Audience;
        Environment = target.Environment;
    }

    /// <summary>Gets the incoming profile version, if one was declared.</summary>
    internal string? ProfileVersion { get; init; } = "1";

    /// <summary>Gets the verified principal kind.</summary>
    internal CallerPrincipalKind PrincipalKind { get; init; }

    /// <summary>Gets the verified credential kind.</summary>
    internal CallerCredentialKind CredentialKind { get; init; }

    /// <summary>Gets the credential identifier, never a raw secret.</summary>
    internal CredentialId CredentialId { get; init; }

    /// <summary>Gets the verified resource.</summary>
    internal string Resource { get; init; }

    /// <summary>Gets the verified audience.</summary>
    internal string Audience { get; init; }

    /// <summary>Gets the verified environment.</summary>
    internal string Environment { get; init; }

    /// <summary>Gets the verified tenant, if present.</summary>
    internal TenantId? Tenant { get; init; }

    /// <summary>Gets the confirmed organization binding, if applicable.</summary>
    internal CallerOrganisationId? Organisation { get; init; }

    /// <summary>Gets the verified policy epoch, if organization policy applies. Consumers must re-evaluate current policy.</summary>
    internal ulong? PolicyEpoch { get; init; }

    /// <summary>Gets the verified account, if applicable.</summary>
    internal CallerAccountKey? AccountKey { get; init; }

    /// <summary>Gets the person, if applicable.</summary>
    internal CallerPersonId? Person { get; init; }

    /// <summary>Gets the service, if applicable.</summary>
    internal CallerServicePrincipalId? ServicePrincipal { get; init; }

    /// <summary>Gets the authenticated current actor, if delegated.</summary>
    internal CallerActor? Actor { get; init; }

    /// <summary>Gets the grant from the credential or a credential-id mapping, never from request input.</summary>
    internal CallerGrantBinding? Grant { get; init; }

    /// <summary>Gets the verified person assurance, if applicable.</summary>
    internal CallerAssurance? Assurance { get; init; }

    /// <summary>Gets client metadata, not actor authority.</summary>
    internal CallerClient? Client { get; init; }

    /// <summary>Gets the verified scope ceiling; the builder snapshots this set.</summary>
    internal IReadOnlySet<string> Scopes { get; init; } = new HashSet<string>();

    /// <summary>Gets the explicit permission ceiling; the builder snapshots this set.</summary>
    internal IReadOnlySet<string> Permissions { get; init; } = new HashSet<string>();

    /// <summary>Gets the credential's sender-binding key thumbprint, if present.</summary>
    internal string? Cnf { get; init; }

    /// <summary>Gets the verifier's checked proof and replay outcome.</summary>
    internal CallerProof Proof { get; init; } = new(CallerProofOutcome.NotPresented);
}
