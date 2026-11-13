// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Binds independent unbound OAuth evidence to an already verified signed v2 context.</summary>
internal static class OAuthBearerForwardedVerifier
{
    /// <summary>Verifies a covered token and binds its authoritative account to a persisted person.</summary>
    /// <param name="services">The current request services.</param>
    /// <param name="authorization">The exact covered bearer authorization.</param>
    /// <param name="context">The verified signed context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Bound facts or a terminal refusal.</returns>
    internal static async Task<ForwardedBearerVerification> Verify(IServiceProvider services, string authorization, ForwardedIdentityV2Context context, CancellationToken cancellationToken)
    {
        var presentation = Presentation(services, authorization, context);
        if (presentation is null) return ForwardedBearerVerification.Refused();
        var result = await services.GetRequiredService<IOAuthBearerVerifier>().Verify(presentation, cancellationToken);
        if (result?.Value is not { } evidence || !Matches(evidence, presentation)) return ForwardedBearerVerification.Refused();
        var account = new CallerAccountKey(evidence.Issuer, evidence.Subject);
        var found = await services.GetRequiredService<ICallerPersonDirectory>().Find(account, cancellationToken);
        if (found.Person is not { } person) return ForwardedBearerVerification.Refused();
        return ForwardedBearerVerification.Accepted(ForwardedBearerVerifierKind.OAuth, account, Facts(context, presentation, evidence, person));
    }

    static OAuthBearerPresentation? Presentation(IServiceProvider services, string authorization, ForwardedIdentityV2Context context)
    {
        if (!IsEnabled(services, context) || !ValidMetadata(context)) return null;
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;
        var installation = services.GetRequiredService<VerifiedCallerInstallation>();
        var settings = services.GetRequiredService<ForwardedIdentityV2Settings>();
        if (installation.Environment is null) return null;
        var target = new CallerCredentialTarget(settings.Resource, settings.Audience, installation.Environment);
        return new(authorization[7..], context, target, context.TokenScope?.Split(' ').ToFrozenSet(StringComparer.Ordinal));
    }

    static bool IsEnabled(IServiceProvider services, ForwardedIdentityV2Context context) =>
        services.GetService<OAuthCapableCallerInstallation>() is not null && context.CredentialKind == CallerCredentialKind.OAuthGrant && OAuthPatCapabilityPolicy.AllowsContext(context);

    static VerifiedCredentialFacts Facts(ForwardedIdentityV2Context context, OAuthBearerPresentation presentation, OAuthBearerVerified evidence, CallerPersonId person) =>
        new(CallerPrincipalKind.Person, CallerCredentialKind.OAuthGrant, new(evidence.CredentialId), new(presentation.Resource, presentation.Audience, presentation.Environment))
        {
            AccountKey = new(evidence.Issuer, evidence.Subject),
            Person = person,
            Assurance = new(context.Idp!, context.AuthTime!.Value, context.Acr, context.Amr),
            PolicyEpoch = context.PolicyEpoch,
            Tenant = presentation.Tenant is { } tenant ? new TenantId(tenant) : null,
            Scopes = evidence.Scopes,
            Client = evidence.ClientId is { } client ? new CallerClient(client, CallerClientAuthentication.Declared) : null,
            Cnf = null,
            Proof = new(CallerProofOutcome.NotPresented)
        };

    static bool ValidMetadata(ForwardedIdentityV2Context context)
    {
        if (context.TenantTargets.Any(string.IsNullOrEmpty) || context.TenantTargets.Distinct(StringComparer.Ordinal).Skip(1).Any()) return false;
        if (context.TokenClientId is { Length: 0 }) return false;
        if (context.TokenScope is null) return true;
        var tokens = context.TokenScope.Split(' ');
        return tokens.Distinct(StringComparer.Ordinal).Count() == tokens.Length && tokens.All(ValidScope);
    }

    static bool ValidScope(string token) => token.Length > 0 && token.All(character => character is >= (char)0x21 and <= (char)0x7e and not '"' and not '\\');

    static bool Matches(OAuthBearerVerified evidence, OAuthBearerPresentation expected) =>
        MatchesAccount(evidence, expected) && evidence.ConfirmationKeyThumbprint is null && MatchesMetadata(evidence, expected);

    static bool MatchesAccount(OAuthBearerVerified evidence, OAuthBearerPresentation expected) =>
        evidence.Issuer == expected.Issuer && evidence.Subject == expected.Subject && evidence.CredentialId == expected.CredentialId;

    static bool MatchesMetadata(OAuthBearerVerified evidence, OAuthBearerPresentation expected) =>
        evidence.Scopes.All(ValidScope) && (expected.Scopes is null || evidence.Scopes.All(expected.Scopes.Contains)) && MatchesClient(evidence.ClientId, expected.ClientId);

    static bool MatchesClient(string? verified, string? signed) => verified is not { Length: 0 } && (signed is null || verified == signed);
}
