// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Checks current authority, issues with an atomic commit, and revokes with an atomic owner check.</summary>
/// <param name="settings">Validated installation limits.</param>
/// <param name="authority">Authoritative persistence.</param>
/// <param name="epochs">Current recovery authority.</param>
/// <param name="policy">Current policy.</param>
/// <param name="context">Verified request context.</param>
/// <param name="clock">Time source.</param>
/// <param name="oauthInstallations">Explicit OAuth/PAT compositions, which do not permit OAuth-to-PAT exchange.</param>
internal sealed class PersonalAccessTokens(
    PatSettings settings,
    IPatAuthority authority,
    IPatRecoveryEpoch epochs,
    IPatPolicy policy,
    IPatIssuanceContext context,
    TimeProvider clock,
    IEnumerable<OAuthCapableCallerInstallation> oauthInstallations) : IPersonalAccessTokens
{
    /// <inheritdoc/>
    public async Task<PatIssueResult> Issue(TimeSpan lifetime, IEnumerable<string> scopes, CancellationToken cancellationToken = default)
    {
        IssuedPat? issued = null;
        try
        {
            var now = clock.GetUtcNow();
            var caller = await context.GetCurrent(cancellationToken);
            if (caller?.CredentialKind == CallerCredentialKind.OAuthGrant && oauthInstallations.Any()) return Refused("pat_issuance_refused");
            var requested = scopes.ToFrozenSet(StringComparer.Ordinal);
            if (!CanIssue(caller, lifetime, requested, now)) return Refused("pat_issuance_refused");
            var epoch = await epochs.Read(cancellationToken);
            if (epoch is null) return Refused("pat_authority_unavailable");
            var assurance = caller!.Assurance! with
            {
                Acr = caller.Assurance!.Acr ?? "basic",
                Amr = caller.Assurance.Amr is null ? null : Array.AsReadOnly(caller.Assurance.Amr.ToArray())
            };
            var record = new PatRecord(
                new(Guid.NewGuid().ToString("D")),
                string.Empty,
                settings.ResourceKey.Value,
                settings.Target.Resource,
                settings.Target.Audience,
                settings.Target.Environment,
                caller.Person!,
                caller.AccountKey!,
                caller.Tenant!,
                caller.Organisation,
                requested,
                now,
                now + lifetime,
                assurance,
                epoch,
                null);
            var decision = await policy.Evaluate(record, true, cancellationToken);
            if (!PatValidation.Permits(record, decision)) return Refused("pat_policy_refused");

            // Only after checks generate a secret. The port receives only its digest and metadata.
            var raw = PatFormat.Create(settings.ResourceKey).Reveal();
            record = record with { Verifier = PatVerifier.Derive(raw), PolicyEpoch = decision.PolicyEpoch };
            issued = new(raw, record.CredentialId, record.ExpiresAt);
            if (!await authority.TryCreate(record, new(record.CredentialId, now), cancellationToken))
            {
                return Refused("pat_commit_refused");
            }
            var result = new PatIssueResult(issued, null);
            issued = null;
            return result;
        }
        catch (Exception)
        {
            // Never include adapter exception text: it may contain credential material.
            return Refused("pat_authority_unavailable");
        }
        finally
        {
            issued?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<PatRevokeResult> Revoke(CredentialId credentialId, CancellationToken cancellationToken = default)
    {
        try
        {
            var caller = await context.GetCurrent(cancellationToken);
            if (!PatValidation.HasOwner(caller, settings) || string.IsNullOrWhiteSpace(credentialId?.Value)) return PatRevokeResult.Refused;
            if (caller!.CredentialKind != CallerCredentialKind.PersonalAccessToken) return PatRevokeResult.Refused;
            return await authority.TryRevoke(new(settings.Target.Resource, settings.Target.Environment, caller!.Person!, caller.AccountKey!, caller.Tenant!), credentialId, cancellationToken);
        }
        catch (Exception)
        {
            return PatRevokeResult.Unavailable;
        }
    }

    static PatIssueResult Refused(string code) => new(null, code);

    bool CanIssue(VerifiedCaller? caller, TimeSpan lifetime, IReadOnlySet<string> scopes, DateTimeOffset now) =>
        PatValidation.HasOwner(caller, settings) && PatValidation.ValidAssurance(caller!.Assurance, now)
        && caller.CredentialKind is CallerCredentialKind.BrowserSession or CallerCredentialKind.OAuthGrant
        && lifetime > TimeSpan.Zero && lifetime <= settings.MaximumLifetime && lifetime <= PatSettings.AbsoluteMaximumLifetime
        && settings.Allows(scopes) && scopes.All(caller.Scopes.Contains);
}
