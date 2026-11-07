// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Authoritative metadata. The verifier is a domain-separated one-way digest, never a bearer value.</summary>
/// <param name="CredentialId">Non-secret identifier.</param>
/// <param name="Verifier">One-way digest.</param>
/// <param name="ResourceKey">Canonical format key.</param>
/// <param name="Resource">Bound resource.</param>
/// <param name="Audience">Bound audience.</param>
/// <param name="Environment">Bound environment.</param>
/// <param name="Person">Verified owner.</param>
/// <param name="AccountKey">Verified upstream account.</param>
/// <param name="Tenant">Verified tenant.</param>
/// <param name="Organisation">Verified organization, if present.</param>
/// <param name="Scopes">Immutable scope ceiling.</param>
/// <param name="CreatedAt">Issuance timestamp.</param>
/// <param name="ExpiresAt">Absolute expiry.</param>
/// <param name="Assurance">Recorded upstream creation-time assurance.</param>
/// <param name="RecoveryEpoch">Authoritative issuance epoch.</param>
/// <param name="PolicyEpoch">Policy snapshot hint, never independent authorization.</param>
/// <param name="Revoked">Authoritative revocation status.</param>
internal sealed record PatRecord(
    CredentialId CredentialId,
    string Verifier,
    string ResourceKey,
    string Resource,
    string Audience,
    string Environment,
    CallerPersonId Person,
    CallerAccountKey AccountKey,
    TenantId Tenant,
    CallerOrganisationId? Organisation,
    IReadOnlySet<string> Scopes,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    CallerAssurance Assurance,
    ulong? RecoveryEpoch,
    ulong? PolicyEpoch,
    bool Revoked = false);
