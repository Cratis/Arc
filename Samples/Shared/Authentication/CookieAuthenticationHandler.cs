// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Cratis.Arc.Identity;
using Cratis.Execution;
using Microsoft.Extensions.Hosting;

namespace Samples.Authentication;

/// <summary>
/// Represents an <see cref="IAuthenticationHandler"/> that reads the Microsoft Identity Platform
/// client principal from a cookie. This enables authentication for transports that cannot send
/// custom HTTP headers, such as Server-Sent Events (EventSource) and WebSocket handshakes.
/// </summary>
/// <remarks>
/// WARNING: This handler trusts an unsigned, client-supplied cookie, so any caller can impersonate any user.
/// It exists for local demos only and must be registered only in the Development environment.
/// Never copy it into an application. Handlers are discovered automatically, so the handler itself
/// ignores every request outside the Development environment.
/// </remarks>
/// <param name="environment">The optional host environment; when absent, the ASPNETCORE_ENVIRONMENT variable decides.</param>
public class CookieAuthenticationHandler(IHostEnvironment? environment = null) : IAuthenticationHandler
{
    /// <summary>
    /// The name of the cookie that carries the base64-encoded client principal.
    /// </summary>
    public const string CookieName = "x-ms-client-principal";

    /// <inheritdoc/>
    public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        if (!(environment?.IsDevelopment() ?? RuntimeEnvironment.IsDevelopment))
        {
            return Task.FromResult(AuthenticationResult.Anonymous);
        }

        if (!context.Cookies.TryGetValue(CookieName, out var cookieValue) ||
            string.IsNullOrWhiteSpace(cookieValue))
        {
            return Task.FromResult(AuthenticationResult.Anonymous);
        }

        ClientPrincipal? clientPrincipal;

        try
        {
            var token = Convert.FromBase64String(cookieValue);
            clientPrincipal = JsonSerializer.Deserialize(token, SamplesJsonSerializerContext.Default.ClientPrincipal);
        }
        catch
        {
            return Task.FromResult(AuthenticationResult.Failed(
                new AuthenticationFailureReason("Invalid x-ms-client-principal cookie value")));
        }

        if (clientPrincipal is null)
        {
            return Task.FromResult(AuthenticationResult.Failed(
                new AuthenticationFailureReason("Unable to deserialize client principal from cookie")));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, clientPrincipal.UserId),
            new("sub", clientPrincipal.UserId),
            new(ClaimTypes.Name, clientPrincipal.UserDetails),
        };

        claims.AddRange(clientPrincipal.UserRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(clientPrincipal.Claims.Select(c => new Claim(c.typ, c.val)));

        var identity = new ClaimsIdentity(claims, "Cookie");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticationResult.Succeeded(principal));
    }
}
