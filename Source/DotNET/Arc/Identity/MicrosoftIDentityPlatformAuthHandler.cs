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
/// The forwarded identity headers are not signed. The handler ignores them and returns no result, so the request stays
/// anonymous, unless <see cref="ArcOptions.TrustForwardedIdentityHeaders"/> is enabled because the host runs behind a
/// trusted ingress.
/// </remarks>
public class MicrosoftIDentityPlatformAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// Gets the scheme name.
    /// </summary>
    public const string SchemeName = "MicrosoftIdentityPlatform";

    readonly ILogger<MicrosoftIDentityPlatformAuthHandler> _logger;
    readonly IOptionsMonitor<ArcOptions>? _arcOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftIDentityPlatformAuthHandler"/> class.
    /// </summary>
    /// <param name="options">The <see cref="IOptionsMonitor{TOptions}"/>.</param>
    /// <param name="arcOptions">The <see cref="ArcOptions"/> that decide whether forwarded identity headers are trusted.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    [ActivatorUtilitiesConstructor]
    public MicrosoftIDentityPlatformAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        IOptionsMonitor<ArcOptions> arcOptions,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder)
        : base(options, loggerFactory, encoder)
    {
        _arcOptions = arcOptions;
        _logger = loggerFactory.CreateLogger<MicrosoftIDentityPlatformAuthHandler>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrosoftIDentityPlatformAuthHandler"/> class,
    /// resolving the host's trust setting from request services when authentication runs.
    /// </summary>
    /// <param name="options">The <see cref="IOptionsMonitor{TOptions}"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="encoder">The <see cref="UrlEncoder"/>.</param>
    [Obsolete("Use the constructor accepting IOptionsMonitor<ArcOptions>. Forwarded identity headers are untrusted unless the host opts in.")]
    public MicrosoftIDentityPlatformAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder)
        : base(options, loggerFactory, encoder)
    {
        _logger = loggerFactory.CreateLogger<MicrosoftIDentityPlatformAuthHandler>();
    }

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var arcOptions = _arcOptions ?? Context.RequestServices.GetService<IOptionsMonitor<ArcOptions>>();
        if (arcOptions?.CurrentValue.TrustForwardedIdentityHeaders != true)
        {
            if (UntrustedForwardedIdentityHeaders.ArePresent(Request.Headers.ContainsKey))
            {
                UntrustedForwardedIdentityHeaders.Report(_logger);
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }

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

        claims.RemoveAll(claim =>
            claim.Type == ClaimTypes.NameIdentifier ||
            claim.Type == "sub" ||
            claim.Type.Equals(MicrosoftIdentityPlatformClaims.IdentityProvider, StringComparison.OrdinalIgnoreCase));
        claims.Add(new Claim(ClaimTypes.Name, clientPrincipal.UserDetails));
        claims.Add(new Claim(ClaimTypes.NameIdentifier, Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader].ToString()));
        claims.Add(new Claim("sub", Request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader].ToString()));

        if (!string.IsNullOrWhiteSpace(clientPrincipal.IdentityProvider))
        {
            claims.Add(new Claim(MicrosoftIdentityPlatformClaims.IdentityProvider, clientPrincipal.IdentityProvider));
        }

        claims.AddRange(clientPrincipal.UserRoles.Select(_ => new Claim(ClaimTypes.Role, _)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
