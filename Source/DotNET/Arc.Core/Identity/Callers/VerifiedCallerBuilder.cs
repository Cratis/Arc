// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers;

/// <summary>Pure construction boundary. Only verified facts may enter; request metadata cannot supply missing identity or grant fields.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="catalog">The explicit resource-to-audience catalog.</param>
internal sealed class VerifiedCallerBuilder(CallerResourceCatalog catalog)
{
    /// <summary>Builds a complete immutable caller or refuses the verified facts.</summary>
    /// <param name="facts">Verified credential facts.</param>
    /// <param name="installation">The configured receiving installation.</param>
    /// <param name="tenant">The independently resolved tenant.</param>
    /// <returns>A caller or a refusal, never both.</returns>
    internal CallerConstruction Build(VerifiedCredentialFacts facts, VerifiedCallerInstallation installation, TenantId? tenant)
    {
        var refusal = CheckProfile(facts.ProfileVersion)
            ?? CheckInstallation(facts, installation, tenant)
            ?? CheckPrincipal(facts)
            ?? CheckGrant(facts)
            ?? CheckProof(facts);
        return refusal is null ? new(CreateCaller(facts), null) : new(null, refusal);
    }

    static VerifiedCaller CreateCaller(VerifiedCredentialFacts facts)
    {
        var client = facts.CredentialKind == CallerCredentialKind.PersonalAccessToken && facts.Client is not null
            ? facts.Client with { Authentication = CallerClientAuthentication.Declared }
            : facts.Client;
        var binding = facts.Cnf is null ? null : new CallerSenderBinding(facts.Cnf, facts.Proof.Jti!);
        return new(
            "1",
            facts.PrincipalKind,
            facts.CredentialKind,
            facts.CredentialId,
            facts.AccountKey,
            facts.Person,
            facts.ServicePrincipal,
            facts.Actor,
            facts.Grant?.Id,
            SnapshotAssurance(facts.Assurance),
            client,
            facts.Resource,
            facts.Tenant,
            facts.Organisation,
            facts.PolicyEpoch,
            facts.Environment,
            facts.Scopes.ToFrozenSet(StringComparer.Ordinal),
            facts.Permissions.ToFrozenSet(StringComparer.Ordinal),
            binding);
    }

    static CallerAssurance? SnapshotAssurance(CallerAssurance? assurance) => assurance is null ? null : assurance with
    {
        Acr = assurance.Acr ?? "basic",
        Amr = assurance.Amr is null ? null : Array.AsReadOnly(assurance.Amr.ToArray())
    };

    static CallerRefusal? CheckProfile(string? version) => VerifiedCallerContract.Accepts(version) ? null : CallerRefusal.UnsupportedProfile;

    static CallerRefusal? CheckPrincipal(VerifiedCredentialFacts facts)
    {
        if (!ValidCoreFields(facts))
        {
            return CallerRefusal.MissingRequiredField;
        }
        return facts.PrincipalKind switch
        {
            CallerPrincipalKind.OrganizationAutomation or CallerPrincipalKind.ClusterWorkload or CallerPrincipalKind.Runner => CheckService(facts),
            CallerPrincipalKind.Person or CallerPrincipalKind.DelegatedActor => CheckPerson(facts),
            _ => CallerRefusal.MissingRequiredField
        };
    }

    static bool ValidCoreFields(VerifiedCredentialFacts facts) =>
        !string.IsNullOrWhiteSpace(facts.CredentialId?.Value) && ValidSet(facts.Scopes) && ValidSet(facts.Permissions) && ValidClient(facts.Client);

    static bool ValidSet(IReadOnlySet<string>? values) => values?.Any(string.IsNullOrWhiteSpace) == false;

    static bool ValidClient(CallerClient? client) => client is null || (!string.IsNullOrWhiteSpace(client.Id) && Enum.IsDefined(client.Authentication));

    static CallerRefusal? CheckPerson(VerifiedCredentialFacts facts)
    {
        if (!ValidPersonFields(facts) || !ValidAssurance(facts.Assurance))
        {
            return CallerRefusal.MissingRequiredField;
        }
        if (!ValidAccount(facts.AccountKey))
        {
            return CallerRefusal.InvalidAccountKey;
        }
        return facts.PrincipalKind == CallerPrincipalKind.Person && HasDelegation(facts) ? CallerRefusal.GrantMismatch : null;
    }

    static bool ValidPersonFields(VerifiedCredentialFacts facts) =>
        facts.CredentialKind is CallerCredentialKind.PersonalAccessToken or CallerCredentialKind.OAuthGrant or CallerCredentialKind.BrowserSession or CallerCredentialKind.OnBehalfSession &&
        !string.IsNullOrWhiteSpace(facts.Person?.Value) && facts.ServicePrincipal is null;

    static bool ValidAssurance(CallerAssurance? assurance) => assurance is not null &&
        !string.IsNullOrWhiteSpace(assurance.Idp) && (assurance.Acr is null || !string.IsNullOrWhiteSpace(assurance.Acr)) && assurance.AuthTime > 0;

    static CallerRefusal? CheckService(VerifiedCredentialFacts facts)
    {
        if (HasPersonFields(facts) || HasDelegation(facts))
        {
            return CallerRefusal.ServiceCarriesPersonFields;
        }
        return facts.CredentialKind is not (CallerCredentialKind.MachinePrincipal or CallerCredentialKind.Workload) || string.IsNullOrWhiteSpace(facts.ServicePrincipal?.Value)
            ? CallerRefusal.MissingRequiredField : null;
    }

    static bool HasPersonFields(VerifiedCredentialFacts facts) => facts.Person is not null || facts.AccountKey is not null || facts.Assurance is not null;

    static bool HasDelegation(VerifiedCredentialFacts facts) => facts.Actor is not null || facts.Grant is not null;

    static bool ValidAccount(CallerAccountKey? account) =>
        account is not null && ValidIssuer(account.Issuer) && CanonicalUuidV4(account.Subject);

    static bool ValidIssuer(string issuer) => Uri.TryCreate(issuer, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;

    static bool CanonicalUuidV4(string subject) => Guid.TryParseExact(subject, "D", out var uuid) &&
        string.Equals(uuid.ToString("D"), subject, StringComparison.Ordinal) && subject[14] == '4' && subject[19] is '8' or '9' or 'a' or 'b';

    static CallerRefusal? CheckGrant(VerifiedCredentialFacts facts)
    {
        if (facts.PrincipalKind != CallerPrincipalKind.DelegatedActor)
        {
            return null;
        }
        if (facts.Grant is null)
        {
            return CallerRefusal.GrantMissing;
        }
        return ValidDelegationIdentity(facts) && GrantMatches(facts.Grant, facts) ? null : CallerRefusal.GrantMismatch;
    }

    static bool ValidDelegationIdentity(VerifiedCredentialFacts facts) => facts.CredentialKind is CallerCredentialKind.OAuthGrant or CallerCredentialKind.OnBehalfSession &&
        !string.IsNullOrWhiteSpace(facts.Actor?.Issuer) && !string.IsNullOrWhiteSpace(facts.Actor.Subject) && !string.IsNullOrWhiteSpace(facts.Grant!.Id?.Value);

    static bool GrantMatches(CallerGrantBinding grant, VerifiedCredentialFacts facts) =>
        grant.AccountKey == facts.AccountKey && grant.Actor == facts.Actor && grant.Tenant == facts.Tenant && GrantTargetMatches(grant, facts);

    static bool GrantTargetMatches(CallerGrantBinding grant, VerifiedCredentialFacts facts) =>
        string.Equals(grant.Resource, facts.Resource, StringComparison.Ordinal) &&
        string.Equals(grant.Audience, facts.Audience, StringComparison.Ordinal) &&
        string.Equals(grant.Environment, facts.Environment, StringComparison.Ordinal);

    static CallerRefusal? CheckProof(VerifiedCredentialFacts facts)
    {
        if (facts.Cnf is null)
        {
            return facts.Proof?.Outcome == CallerProofOutcome.NotPresented ? null : CallerRefusal.SenderBindingMismatch;
        }
        return facts.Proof?.Outcome switch
        {
            null or CallerProofOutcome.NotPresented => CallerRefusal.SenderBindingRequired,
            CallerProofOutcome.Replayed => CallerRefusal.SenderBindingReplayed,
            CallerProofOutcome.Verified => ProofMatches(facts) ? null : CallerRefusal.SenderBindingMismatch,
            _ => CallerRefusal.SenderBindingMismatch
        };
    }

    static bool ProofMatches(VerifiedCredentialFacts facts) => !string.IsNullOrWhiteSpace(facts.Cnf) &&
        !string.IsNullOrWhiteSpace(facts.Proof.Jti) && string.Equals(facts.Proof.KeyThumbprint, facts.Cnf, StringComparison.Ordinal);

    static bool ValidTenant(TenantId? verified, TenantId? resolved) => verified == resolved &&
        (verified is null || !string.IsNullOrWhiteSpace(verified.Value));

    CallerRefusal? CheckInstallation(VerifiedCredentialFacts facts, VerifiedCallerInstallation installation, TenantId? tenant)
    {
        if (!TryGetConfiguredAudience(installation, out var audience))
        {
            return CallerRefusal.InstallationNotConfigured;
        }
        if (!string.Equals(facts.Resource, installation.Resource, StringComparison.Ordinal))
        {
            return CallerRefusal.ResourceMismatch;
        }
        if (!string.Equals(facts.Audience, audience, StringComparison.Ordinal))
        {
            return CallerRefusal.AudienceMismatch;
        }
        if (!string.Equals(facts.Environment, installation.Environment, StringComparison.Ordinal))
        {
            return CallerRefusal.EnvironmentMismatch;
        }
        return ValidTenant(facts.Tenant, tenant) ? null : CallerRefusal.TenantMismatch;
    }

    bool TryGetConfiguredAudience(VerifiedCallerInstallation installation, out string audience)
    {
        audience = string.Empty;
        return !string.IsNullOrWhiteSpace(installation.Resource) && !string.IsNullOrWhiteSpace(installation.Environment) &&
            catalog.TryGetAudience(installation.Resource, out audience) && !string.IsNullOrWhiteSpace(audience);
    }
}
