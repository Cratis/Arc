// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Represents an <see cref="AuthenticationHandler{TOptions}"/> for handling authentication in the context of Microsoft Identity Platform.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MicrosoftIDentityPlatformAuthHandler"/> class.
/// </remarks>
/// <param name="options">The <see cref="IOptionsMonitor{TOptions}"/>.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
/// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
public class MicrosoftIDentityPlatformAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    /// <summary>
    /// Gets the scheme name.
    /// </summary>
    public const string SchemeName = "MicrosoftIdentityPlatform";

    readonly ILogger<MicrosoftIDentityPlatformAuthHandler> _logger = loggerFactory.CreateLogger<MicrosoftIDentityPlatformAuthHandler>();

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.IsValidIdentityRequest())
        {
            return Task.FromResult(AuthenticateResult.Fail("Not authenticated - headers missing"));
        }

        ClientPrincipal? clientPrincipal = null;
        try
        {
            clientPrincipal = Request.GetClientPrincipal();
        }
        catch (Exception ex)
        {
            _logger.FailedResolvingClientPrincipal(ex);
        }

        if (clientPrincipal == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Not authenticated - invalid representation of ClientPrincipal"));
        }

        var claims = clientPrincipal.GetClaims().ToList();

        claims.RemoveAll(claim => claim.Type == ClaimTypes.NameIdentifier || claim.Type == "sub");
        claims.Add(new Claim(ClaimTypes.Name, clientPrincipal.UserDetails));
        claims.Add(new Claim(ClaimTypes.NameIdentifier, Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader].ToString()));
        claims.Add(new Claim("sub", Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader].ToString()));
        claims.AddRange(clientPrincipal.UserRoles.Select(_ => new Claim(ClaimTypes.Role, _)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
