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
/// A request carrying <see cref="OnBehalfOfHeaders.ActorHeader"/> is rejected unless it is a trusted forward of the
/// represented user; when accepted, the actor becomes the <see cref="ClaimsIdentity.Actor"/> of the principal.
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

        if (!options.Value.TrustForwardedIdentityHeaders)
        {
            if (onBehalfOf)
            {
                _logger.UntrustedOnBehalfOfForwardRejected();
                return Task.FromResult(AuthenticationResult.Failed(ForwardedActor.UntrustedForward));
            }

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
            return Task.FromResult(onBehalfOf ? AuthenticationResult.Failed(ForwardedActor.MissingRepresentedUser) : AuthenticationResult.Anonymous);
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
            return Task.FromResult(AuthenticationResult.Failed("Not authenticated - invalid representation of ClientPrincipal"));
        }

        var claims = ForwardedIdentityClaims.From(clientPrincipal, headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader]);
        var identity = new ClaimsIdentity(claims, nameof(MicrosoftIdentityPlatformAuthenticationHandler));
        if (onBehalfOf)
        {
            var failure = ForwardedActor.Attach(
                headers[OnBehalfOfHeaders.ActorHeader],
                identity,
                nameof(MicrosoftIdentityPlatformAuthenticationHandler),
                options.Value.JsonSerializerOptions);
            if (failure is not null)
            {
                return Task.FromResult(AuthenticationResult.Failed(failure));
            }
        }

        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticationResult.Succeeded(principal));
    }
}
