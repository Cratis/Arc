// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Supplies facts only after complete v2 verification and persisted-person resolution.</summary>
/// <param name="requests">The current request accessor.</param>
/// <param name="settings">The explicit installation settings.</param>
/// <param name="verifier">The scoped v2 verifier.</param>
/// <param name="installation">The receiving installation.</param>
/// <param name="persons">The persisted-person directory.</param>
/// <param name="logger">The fixed-reason logger.</param>
/// <param name="hopLogging">The shared hop warning limiter unless explicitly supplied.</param>
internal sealed class ForwardedIdentityV2CredentialSource(
    IHttpRequestContextAccessor requests,
    ForwardedIdentityV2Settings settings,
    ForwardedIdentityV2Verifier verifier,
    VerifiedCallerInstallation installation,
    ICallerPersonDirectory persons,
    ILogger<ForwardedIdentityV2CredentialSource> logger,
    ForwardedIdentityHopLogging? hopLogging = null) : IVerifiedCredentialSource
{
    /// <inheritdoc/>
    PresentedCredentialShape IVerifiedCredentialSource.Shape => PresentedCredentialShape.Envelope;

    /// <inheritdoc/>
    async Task<CredentialVerification> IVerifiedCredentialSource.Verify(CredentialPresentation presentation, CancellationToken cancellationToken)
    {
        if (requests.Current is not { } request || request is not ICanReadForwardedIdentityRequest) return CredentialVerification.Refused(CallerRefusal.UnreadableCredentialHeaders);
        if (!IsHopAdmitted(request)) return Refuse(ForwardedIdentityV2Rejection.Hop);
        var signatures = presentation.Envelope.Values.SelectMany(values => values).ToArray();
        if (signatures.Length != 1) return Refuse(ForwardedIdentityV2Rejection.Format);
        if (!signatures[0].StartsWith("v2.", StringComparison.Ordinal))
        {
            if (!settings.V2Only) return CredentialVerification.NotApplicable();
            return Refuse(ForwardedIdentityV2Rejection.Version);
        }
        var snapshot = ForwardedIdentityV2Request.Capture(request, settings);
        if (snapshot is null) return Refuse(ForwardedIdentityV2Rejection.Headers);
        var result = await verifier.Verify(settings, snapshot, cancellationToken);
        if (result.Rejection is { } reason) return Refuse(reason);
        if (result.Bearer?.Facts is { } bearerFacts) return CredentialVerification.Verified(bearerFacts);
        var context = result.Context!;
        if (context.CredentialKind == CallerCredentialKind.PersonalAccessToken) return Refuse(ForwardedIdentityV2Rejection.Credential);
        if (!IsDirectBrowser(context)) return CredentialVerification.Refused(CallerRefusal.UnsupportedProfile);
        if (context.TenantTargets.Any(string.IsNullOrEmpty) || context.TenantTargets.Distinct(StringComparer.Ordinal).Skip(1).Any()) return CredentialVerification.Refused(CallerRefusal.TenantMismatch);
        TenantId? tenant = context.TenantTargets.Count == 0 ? null : new(context.TenantTargets[0]);
        if (!TryScopes(context.TokenScope, out var scopes) || context.TokenClientId is { Length: 0 }) return Refuse(ForwardedIdentityV2Rejection.Metadata);
        var client = context.TokenClientId is { Length: > 0 } id ? new CallerClient(id, CallerClientAuthentication.Declared) : null;
        var account = new CallerAccountKey(context.Issuer!, context.Subject!);
        var found = await persons.Find(account, cancellationToken);
        if (found.Person is not { } person) return CredentialVerification.Refused(CallerRefusal.MissingRequiredField);
        return CredentialVerification.Verified(new(CallerPrincipalKind.Person, CallerCredentialKind.BrowserSession, new(context.CredentialId), new(settings.Resource, settings.Audience, installation.Environment!))
        {
            AccountKey = account,
            Person = person,
            Assurance = new(context.Idp!, context.AuthTime!.Value, context.Acr, context.Amr),
            PolicyEpoch = context.PolicyEpoch,
            Tenant = tenant,
            Scopes = scopes,
            Client = client,
            Cnf = context.SenderBinding.KeyThumbprint,
            Proof = context.SenderBinding
        });
    }

    /// <inheritdoc/>
    bool IVerifiedCredentialSource.Consumes(string headerName) => ForwardedIdentityV2Canonicalization.IsCovered(headerName, settings.TenantHeader);

    static bool IsDirectBrowser(ForwardedIdentityV2Context context) => context.CredentialKind == CallerCredentialKind.BrowserSession && context.Actor is null && context.GrantId is null;

    static bool TryScopes(string? value, out FrozenSet<string> scopes)
    {
        var tokens = value?.Split(' ') ?? [];
        scopes = tokens.ToFrozenSet(StringComparer.Ordinal);
        return scopes.Count == tokens.Length && tokens.All(token => token.Length > 0 && token.All(character => character is >= (char)0x21 and <= (char)0x7e and not '"' and not '\\'));
    }

    bool IsHopAdmitted(IHttpRequestContext request)
    {
        var hop = request.RequestServices?.GetService<IOptions<ArcOptions>>() is { } options
            ? ForwardedIdentityHop.Admit(request, options.Value)
            : ForwardedIdentityHopAdmission.NotRequired;
        if (hop is not (ForwardedIdentityHopAdmission.NotRequired or ForwardedIdentityHopAdmission.Authenticated) && ForwardedIdentityHeaders.ArePresent(request))
        {
            (hopLogging ?? ForwardedIdentityHopLogging.Shared).Report(logger, hop, request);
        }
        return hop == ForwardedIdentityHopAdmission.Authenticated;
    }

    CredentialVerification Refuse(ForwardedIdentityV2Rejection reason)
    {
        logger.Rejected(reason);
        return CredentialVerification.Refused(CallerRefusal.SourceRefused);
    }
}
