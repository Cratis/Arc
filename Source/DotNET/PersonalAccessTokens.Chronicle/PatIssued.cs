// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.PersonalAccessTokens.Chronicle;

/// <summary>Audit-only fact of committed issuance. Chronicle is not the credential or revocation authority.</summary>
/// <param name="CredentialId">Non-secret credential identifier.</param>
/// <param name="Resource">Resource binding.</param>
/// <param name="Environment">Environment binding.</param>
/// <param name="Tenant">Owner's tenant.</param>
/// <param name="Person">Owner's person identifier.</param>
/// <param name="Scopes">Creation-time scope ceiling, not current authorization.</param>
/// <param name="ExpiresAt">Expiry timestamp.</param>
/// <param name="Assurance">Recorded upstream creation-time assurance.</param>
/// <param name="RestoreEpoch">Recovery epoch at issuance.</param>
/// <param name="VerifierId">Domain-separated hash identifier; never a token, secret or full verifier.</param>
/// <param name="IssuedAt">Authority's issuance timestamp.</param>
[EventType]
public sealed record PatIssued(
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
    DateTimeOffset IssuedAt);
