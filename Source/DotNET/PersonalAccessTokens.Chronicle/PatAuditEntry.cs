// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.PersonalAccessTokens.Chronicle;

/// <summary>Audit-only listing state, keyed by credential. Never use it as verification or revocation authority.</summary>
/// <remarks>Owner listings must filter by resource, environment, tenant and verified person, with application authorization.</remarks>
/// <param name="CredentialId">Non-secret credential identifier.</param>
/// <param name="Resource">Resource binding.</param>
/// <param name="Environment">Environment binding.</param>
/// <param name="Tenant">Owner's tenant.</param>
/// <param name="Person">Owner's person identifier.</param>
/// <param name="Scopes">Creation-time scope ceiling.</param>
/// <param name="ExpiresAt">Expiry timestamp.</param>
/// <param name="Assurance">Creation-time upstream assurance.</param>
/// <param name="RestoreEpoch">Recovery epoch at issuance.</param>
/// <param name="VerifierId">Domain-separated hash identifier, not the full verifier.</param>
/// <param name="IssuedAt">Authority's issuance timestamp.</param>
/// <param name="RevokedAt">Authority's revocation timestamp, if delivered.</param>
public sealed record PatAuditEntry(
    CredentialId CredentialId,
    string Resource,
    string Environment,
    TenantId Tenant,
    CallerPersonId Person,
    IReadOnlyList<string> Scopes,
    DateTimeOffset ExpiresAt,
    CallerAssurance Assurance,
    ulong RestoreEpoch,
    string VerifierId,
    DateTimeOffset IssuedAt,
    DateTimeOffset? RevokedAt);
