// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents an <see cref="IAuthenticationHandler"/> for handling authentication in the context of Microsoft Identity Platform.
/// </summary>
/// <remarks>
/// The handler ignores forwarded identity unless <see cref="ArcOptions.TrustForwardedIdentityHeaders"/> is enabled.
/// The default deployment profile requires signed forwarding; unsigned headers are available only with an explicit
/// <see cref="DeploymentProfile.LocalDevelopment"/> profile behind a trusted local ingress.
/// A request carrying <see cref="OnBehalfOfHeaders.ActorHeader"/> or <see cref="OnBehalfOfHeaders.AssertionHeader"/> fails
/// authentication unless <see cref="OnBehalfOfOptions.Enabled"/> is set and its signed assertion verifies; when accepted,
/// the actor becomes the <see cref="ClaimsIdentity.Actor"/> of the principal.
/// </remarks>
/// <param name="options">The <see cref="ArcOptions"/>.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
public class MicrosoftIdentityPlatformAuthenticationHandler(
    IOptions<ArcOptions> options,
    ILoggerFactory loggerFactory) : IAuthenticationHandler
{
    readonly ILogger<MicrosoftIdentityPlatformAuthenticationHandler> _logger = loggerFactory.CreateLogger<MicrosoftIdentityPlatformAuthenticationHandler>();

    /// <inheritdoc/>
    public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        var hop = ForwardedIdentityHop.Admit(context, options.Value);
        if (hop is not (ForwardedIdentityHopAdmission.NotRequired or ForwardedIdentityHopAdmission.Authenticated))
        {
            if (ForwardedIdentityHeaders.ArePresent(context))
            {
                ForwardedIdentityHopLogging.Shared.Report(_logger, hop, context);
                ForwardedIdentityDiagnostics.Shared.Refused(_logger, context, "hop");
            }
            return Task.FromResult(AuthenticationResult.Anonymous);
        }

        var policy = ForwardedIdentityPolicy.Read(options.Value);
        if (ForwardedIdentityHeaders.IsV2(context) || (options.Value.TrustForwardedIdentityHeaders && !policy.UnsignedV1 && !policy.SignedV1)) return Task.FromResult(AuthenticationResult.Anonymous);
        if (policy.SignedV1)
        {
            var signed = ForwardedIdentityAuthentication.Authenticate(context, options.Value, nameof(MicrosoftIdentityPlatformAuthenticationHandler), context.Path);
            if (signed.Failure is { } failure)
            {
                ForwardedIdentityDiagnostics.Shared.Refused(_logger, context, failure);
                return Task.FromResult(AuthenticationResult.Failed(failure));
            }
            if (signed.Principal is not { } signedPrincipal) return Task.FromResult(AuthenticationResult.Anonymous);
            ForwardedIdentityDiagnostics.Shared.Accepted(_logger, context, "signed-v1");
            return Task.FromResult(AuthenticationResult.Succeeded(signedPrincipal));
        }

        var headers = context.Headers;

        var onBehalfOf = ForwardedActor.IsPresent(headers.ContainsKey);
        if (onBehalfOf && ForwardedActor.Admit(options.Value) is { } rejection)
        {
            return Reject(rejection);
        }

        if (!options.Value.TrustForwardedIdentityHeaders)
        {
            if (UntrustedForwardedIdentityHeaders.ArePresent(headers.ContainsKey))
            {
                UntrustedForwardedIdentityHeaders.Report(_logger);
                ForwardedIdentityDiagnostics.Shared.Refused(_logger, context, "disabled");
            }

            return Task.FromResult(AuthenticationResult.Anonymous);
        }

        if (!headers.ContainsKey(MicrosoftIdentityPlatformHeaders.IdentityIdHeader) ||
            !headers.ContainsKey(MicrosoftIdentityPlatformHeaders.IdentityNameHeader) ||
            !headers.ContainsKey(MicrosoftIdentityPlatformHeaders.PrincipalHeader))
        {
            return onBehalfOf ? Reject(OnBehalfOfFailures.MissingRepresentedUser) : Task.FromResult(AuthenticationResult.Anonymous);
        }

        ClientPrincipal? clientPrincipal = null;
        try
        {
            var tokenAsString = headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader];
            if (!string.IsNullOrEmpty(tokenAsString))
            {
                var token = Convert.FromBase64String(tokenAsString);
                clientPrincipal = JsonSerializer.Deserialize(token, options.Value.JsonSerializerOptions.ResolveTypeInfo<ClientPrincipal>());
            }
        }
        catch (Exception ex)
        {
            _logger.FailedResolvingClientPrincipal(ex);
        }

        if (clientPrincipal == null)
        {
            const string invalidPrincipal = "Not authenticated - invalid representation of ClientPrincipal";
            ForwardedIdentityDiagnostics.Shared.Refused(_logger, context, "principal");
            return onBehalfOf ? Reject(invalidPrincipal) : Task.FromResult(AuthenticationResult.Failed(invalidPrincipal));
        }

        var claims = ForwardedIdentityClaims.From(clientPrincipal, headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader]);
        var identity = new ClaimsIdentity(claims, nameof(MicrosoftIdentityPlatformAuthenticationHandler));
        if (onBehalfOf)
        {
            var failure = ForwardedActor.Attach(
                ForwardFrom(context),
                clientPrincipal,
                identity,
                nameof(MicrosoftIdentityPlatformAuthenticationHandler),
                options.Value,
                TimeProvider.System,
                OnBehalfOfReplayCache.Shared);
            if (failure is not null)
            {
                return Reject(failure);
            }
        }

        var principal = new ClaimsPrincipal(identity);

        ForwardedIdentityDiagnostics.Shared.Accepted(_logger, context, "unsigned-v1");
        return Task.FromResult(AuthenticationResult.Succeeded(principal));
    }

    OnBehalfOfForward ForwardFrom(IHttpRequestContext context) => new(
        context.Headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader],
        context.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader],
        context.Headers.GetValueOrDefault(OnBehalfOfHeaders.ActorHeader),
        context.Headers.GetValueOrDefault(OnBehalfOfHeaders.AssertionHeader),
        options.Value.Tenancy?.HttpHeader is { Length: > 0 } tenantHeader ? context.Headers.GetValueOrDefault(tenantHeader) : null,
        context.Method,
        context.Path);

    Task<AuthenticationResult> Reject(string reason)
    {
        RejectedOnBehalfOfForwards.Report(_logger, reason);
        return Task.FromResult(AuthenticationResult.Failed(reason));
    }
}
