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
            return Task.FromResult(AuthenticationResult.Anonymous);
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

        var claims = clientPrincipal.Claims.Select(c => new Claim(c.typ, c.val)).ToList();

        claims.RemoveAll(claim =>
            claim.Type == ClaimTypes.NameIdentifier ||
            claim.Type == "sub" ||
            claim.Type.Equals(MicrosoftIdentityPlatformClaims.IdentityProvider, StringComparison.OrdinalIgnoreCase));
        claims.Add(new Claim(ClaimTypes.Name, clientPrincipal.UserDetails));
        claims.Add(new Claim(ClaimTypes.NameIdentifier, headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader]));
        claims.Add(new Claim("sub", headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader]));

        if (!string.IsNullOrWhiteSpace(clientPrincipal.IdentityProvider))
        {
            claims.Add(new Claim(MicrosoftIdentityPlatformClaims.IdentityProvider, clientPrincipal.IdentityProvider));
        }

        claims.AddRange(clientPrincipal.UserRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, nameof(MicrosoftIdentityPlatformAuthenticationHandler));
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticationResult.Succeeded(principal));
    }
}
