// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Allowlisted audit metadata; the full verifier is deliberately absent.</summary>
/// <param name="CredentialId">Non-secret credential identifier.</param>
/// <param name="Resource">Resource binding.</param>
/// <param name="Environment">Environment binding.</param>
/// <param name="Tenant">Owner's tenant.</param>
/// <param name="Person">Owner's person identifier.</param>
/// <param name="Scopes">Creation-time scope ceiling.</param>
/// <param name="ExpiresAt">Expiry timestamp.</param>
/// <param name="Assurance">Creation-time upstream assurance.</param>
/// <param name="RestoreEpoch">Recovery epoch at issuance.</param>
/// <param name="VerifierId">Domain-separated hash of the verifier, never the verifier itself.</param>
internal sealed record PatAuditMetadata(
    CredentialId CredentialId,
    string Resource,
    string Environment,
    TenantId Tenant,
    CallerPersonId Person,
    IReadOnlyList<string> Scopes,
    DateTimeOffset ExpiresAt,
    CallerAssurance Assurance,
    ulong RestoreEpoch,
    string VerifierId)
{
    /// <summary>Captures an audit snapshot without carrying verifier material into the outbox.</summary>
    /// <param name="record">The authority's record.</param>
    /// <returns>Allowlisted metadata with copied collections.</returns>
    /// <exception cref="InvalidOperationException">The record has no committed recovery epoch.</exception>
    public static PatAuditMetadata From(PatRecord record) => new(
        record.CredentialId,
        record.Resource,
        record.Environment,
        record.Tenant,
        record.Person,
        record.Scopes.ToArray(),
        record.ExpiresAt,
        record.Assurance with { Amr = record.Assurance.Amr?.ToArray() },
        record.RecoveryEpoch ?? throw new InvalidOperationException("An audit snapshot requires a committed recovery epoch."),
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"cratis:arc:pat:audit-verifier:v1:{record.Verifier}"))).ToLowerInvariant());
}
