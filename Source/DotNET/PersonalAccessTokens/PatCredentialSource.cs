// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Claims only PAT namespace values. Every claimed failure is terminal, including authority uncertainty.</summary>
/// <param name="settings">Validated receiving installation.</param>
/// <param name="authority">Authoritative metadata, never a positive cache.</param>
/// <param name="epochs">Current recovery authority.</param>
/// <param name="policy">Current policy.</param>
/// <param name="clock">Time source.</param>
internal sealed class PatCredentialSource(PatSettings settings, IPatAuthority authority, IPatRecoveryEpoch epochs, IPatPolicy policy, TimeProvider clock) : IVerifiedCredentialSource
{
    /// <inheritdoc/>
    public PresentedCredentialShape Shape => PresentedCredentialShape.PatBearer;

    /// <inheritdoc/>
    public bool Consumes(string headerName) => false;

    /// <inheritdoc/>
    public async Task<CredentialVerification> Verify(CredentialPresentation presentation, CancellationToken cancellationToken)
    {
        var authorization = presentation.Authorization.FirstOrDefault(value => PatNamespace.Classify(Extract(value)) == Shape);
        if (authorization is null) return CredentialVerification.NotApplicable();
        if (presentation.Refusal is not null || presentation.Authorization.Count != 1 || presentation.Envelope.Count != 0
            || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Refuse();
        }
        var raw = authorization[7..];
        if (!PatFormat.TryParse(raw, out var parsed) || parsed.ResourceKey != settings.ResourceKey) return Refuse();
        try
        {
            var digest = PatVerifier.Derive(raw);

            // No positive cache: re-read revocation and recovery state on every request.
            var record = await authority.ReadAuthoritative(settings.Target.Resource, digest, cancellationToken);
            if (record is null || !PatVerifier.Matches(digest, record.Verifier)) return Refuse();
            record = record with
            {
                Scopes = record.Scopes.ToFrozenSet(StringComparer.Ordinal),
                Assurance = record.Assurance with { Amr = record.Assurance.Amr is null ? null : Array.AsReadOnly(record.Assurance.Amr.ToArray()) }
            };
            if (!PatValidation.Active(record, settings, clock.GetUtcNow())) return Refuse();
            var epoch = await epochs.Read(cancellationToken);
            if (epoch is null || record.RecoveryEpoch != epoch) return Refuse();
            var decision = await policy.Evaluate(record, false, cancellationToken);
            if (!PatValidation.Permits(record, decision)) return Refuse();
            return CredentialVerification.Verified(new(CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, record.CredentialId, new(record.Resource, record.Audience, record.Environment))
            {
                Person = record.Person,
                AccountKey = record.AccountKey,
                Tenant = record.Tenant,
                Organisation = record.Organisation,
                Assurance = record.Assurance,
                Scopes = record.Scopes,
                PolicyEpoch = decision.PolicyEpoch
            });
        }
        catch (Exception)
        {
            return Refuse();
        }
    }

    static CredentialVerification Refuse() => CredentialVerification.Refused(CallerRefusal.SourceRefused);

    static string Extract(string authorization)
    {
        var separator = authorization.IndexOf(' ');
        return separator < 0 ? authorization : authorization[(separator + 1)..];
    }
}
