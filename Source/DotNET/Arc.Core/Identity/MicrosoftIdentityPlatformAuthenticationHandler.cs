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
/// The forwarded identity headers are not signed. The handler ignores them, and the request stays anonymous, unless
/// <see cref="ArcOptions.TrustForwardedIdentityHeaders"/> is enabled because the host runs behind a trusted ingress.
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
