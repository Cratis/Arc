// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// An actual ASP.NET Core authentication handler for testing distinct default and selected identities.
/// </summary>
/// <param name="options">The scheme options.</param>
/// <param name="logger">The handler logger.</param>
/// <param name="encoder">The URL encoder.</param>
public class ScenarioAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = $"X-{Scheme.Name}";
        if (!Request.Headers.TryGetValue(header, out var value) || value != "active")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var name = Request.Headers.TryGetValue("X-User", out var user) ? user.ToString() : Scheme.Name;
        var claims = new List<Claim> { new(ClaimTypes.Name, name), new("membership", "active") };
        if (Request.Headers.TryGetValue($"X-{Scheme.Name}-Tenant", out var tenant))
        {
            claims.Add(new Claim("tenant_id", tenant.ToString()));
        }

        if (Request.Headers.TryGetValue("X-Permission-Audit", out var audit) && audit == "true")
        {
            claims.Add(new Claim("permission", "audit"));
        }

        if (Request.Headers.TryGetValue("X-Role", out var role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = Request.Headers.ContainsKey("X-Custom-Principal")
            ? new ScenarioPrincipal(identity)
            : new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
