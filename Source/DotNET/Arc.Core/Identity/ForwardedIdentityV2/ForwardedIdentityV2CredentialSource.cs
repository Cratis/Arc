// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Supplies facts only after complete v2 verification and persisted-person resolution.</summary>
/// <param name="requests">The current request accessor.</param>
/// <param name="settings">The explicit installation settings.</param>
/// <param name="verifier">The scoped v2 verifier.</param>
/// <param name="installation">The receiving installation.</param>
/// <param name="persons">The persisted-person directory.</param>
/// <param name="logger">The fixed-reason logger.</param>
internal sealed class ForwardedIdentityV2CredentialSource(
    IHttpRequestContextAccessor requests,
    ForwardedIdentityV2Settings settings,
    ForwardedIdentityV2Verifier verifier,
    VerifiedCallerInstallation installation,
    ICallerPersonDirectory persons,
    ILogger<ForwardedIdentityV2CredentialSource> logger) : IVerifiedCredentialSource
{
    /// <inheritdoc/>
    PresentedCredentialShape IVerifiedCredentialSource.Shape => PresentedCredentialShape.Envelope;

    /// <inheritdoc/>
    async Task<CredentialVerification> IVerifiedCredentialSource.Verify(CredentialPresentation presentation, CancellationToken cancellationToken)
    {
        if (requests.Current is not { } request || request is not ICanReadForwardedIdentityRequest) return CredentialVerification.Refused(CallerRefusal.UnreadableCredentialHeaders);
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
        var account = new CallerAccountKey(context.Issuer!, context.Subject!);
        var found = await persons.Find(account, cancellationToken);
        if (found.Person is not { } person) return CredentialVerification.Refused(CallerRefusal.MissingRequiredField);
        return CredentialVerification.Verified(new(CallerPrincipalKind.Person, CallerCredentialKind.BrowserSession, new(context.CredentialId), new(settings.Resource, settings.Audience, installation.Environment!))
        {
            AccountKey = account,
            Person = person,
            Assurance = new(context.Idp!, context.AuthTime!.Value, context.Acr, context.Amr),
            PolicyEpoch = context.PolicyEpoch,
            Cnf = context.SenderBinding.KeyThumbprint,
            Proof = context.SenderBinding
        });
    }

    static bool IsDirectBrowser(ForwardedIdentityV2Context context) => context.CredentialKind == CallerCredentialKind.BrowserSession && context.Actor is null && context.GrantId is null;

    CredentialVerification Refuse(ForwardedIdentityV2Rejection reason)
    {
        logger.Rejected(reason);
        return CredentialVerification.Refused(CallerRefusal.SourceRefused);
    }
}
