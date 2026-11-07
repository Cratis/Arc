// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Shared non-overridable PAT checks, before current policy can permit anything.</summary>
internal static class PatValidation
{
    /// <summary>Checks a current verified person is bound to this receiving installation.</summary>
    /// <param name="caller">Verified caller.</param>
    /// <param name="settings">Receiving installation.</param>
    /// <returns>Whether the complete owner binding is present.</returns>
    internal static bool HasOwner(VerifiedCaller? caller, PatSettings settings) => caller is
    {
        PrincipalKind: CallerPrincipalKind.Person,
        Person: not null,
        AccountKey: not null,
        Tenant: not null
    } && caller.Resource == settings.Target.Resource && caller.Environment == settings.Target.Environment;

    /// <summary>Checks recorded upstream authentication without refreshing its timestamp.</summary>
    /// <param name="assurance">Verified upstream assurance.</param>
    /// <param name="createdAt">Credential creation timestamp.</param>
    /// <returns>Whether upstream assurance can be retained.</returns>
    internal static bool ValidAssurance(CallerAssurance? assurance, DateTimeOffset createdAt) => assurance is not null
        && !string.IsNullOrWhiteSpace(assurance.Idp) && assurance.AuthTime > 0 && assurance.AuthTime <= createdAt.ToUnixTimeSeconds();

    /// <summary>Checks current status, absolute lifetime and installation binding before policy evaluation.</summary>
    /// <param name="record">Authoritative metadata.</param>
    /// <param name="settings">Receiving installation.</param>
    /// <param name="now">Current time.</param>
    /// <returns>Whether structural and immutable checks pass.</returns>
    internal static bool Active(PatRecord record, PatSettings settings, DateTimeOffset now) => !record.Revoked
        && record.ResourceKey == settings.ResourceKey.Value && record.Resource == settings.Target.Resource
        && record.Audience == settings.Target.Audience && record.Environment == settings.Target.Environment
        && record.CreatedAt <= now && record.ExpiresAt > now && record.ExpiresAt > record.CreatedAt
        && record.ExpiresAt - record.CreatedAt <= settings.MaximumLifetime
        && record.ExpiresAt - record.CreatedAt <= PatSettings.AbsoluteMaximumLifetime
        && settings.Allows(record.Scopes) && ValidAssurance(record.Assurance, record.CreatedAt)
        && !string.IsNullOrWhiteSpace(record.CredentialId?.Value) && !string.IsNullOrWhiteSpace(record.Person?.Value)
        && !string.IsNullOrWhiteSpace(record.Tenant?.Value) && ValidAccount(record.AccountKey)
        && (record.Organisation is null || !string.IsNullOrWhiteSpace(record.Organisation.Value));

    /// <summary>Applies the current policy and its potentially shorter lifetime ceiling.</summary>
    /// <param name="record">Checked credential metadata.</param>
    /// <param name="decision">Current policy decision.</param>
    /// <returns>Whether policy permits this credential.</returns>
    internal static bool Permits(PatRecord record, PatPolicyDecision decision) => decision.Permitted
        && decision.MaximumLifetime > TimeSpan.Zero && record.ExpiresAt - record.CreatedAt <= decision.MaximumLifetime;

    static bool ValidAccount(CallerAccountKey? account) => account is not null && !string.IsNullOrWhiteSpace(account.Issuer)
        && Guid.TryParseExact(account.Subject, "D", out var subject) && account.Subject == subject.ToString("D");
}
