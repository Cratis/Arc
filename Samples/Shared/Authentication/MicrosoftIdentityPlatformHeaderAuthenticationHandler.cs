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
/// Represents an <see cref="IAuthenticationHandler"/> that reads Microsoft Identity Platform headers
/// for use in Arc.Core-based applications (non-ASP.NET Core).
/// </summary>
/// <remarks>
/// WARNING: This handler trusts unsigned, client-supplied headers, so any caller can impersonate any user.
/// It simulates a trusted ingress for local demos only. Never copy it into an application. Handlers are
/// discovered automatically, so the handler itself ignores every request outside the Development environment.
/// </remarks>
/// <param name="environment">The optional host environment; when absent, the ASPNETCORE_ENVIRONMENT variable decides.</param>
public class MicrosoftIdentityPlatformHeaderAuthenticationHandler(IHostEnvironment? environment = null) : IAuthenticationHandler
{
    /// <inheritdoc/>
    public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
    {
        if (!(environment?.IsDevelopment() ?? RuntimeEnvironment.IsDevelopment))
        {
            return Task.FromResult(AuthenticationResult.Anonymous);
        }

        if (!context.Headers.TryGetValue(MicrosoftIdentityPlatformHeaders.IdentityIdHeader, out var userId) ||
            !context.Headers.TryGetValue(MicrosoftIdentityPlatformHeaders.IdentityNameHeader, out var userName) ||
            !context.Headers.TryGetValue(MicrosoftIdentityPlatformHeaders.PrincipalHeader, out var principalHeader))
        {
            return Task.FromResult(AuthenticationResult.Anonymous);
        }

        ClientPrincipal? clientPrincipal = null;
        try
        {
            var token = Convert.FromBase64String(principalHeader);
            clientPrincipal = JsonSerializer.Deserialize(token, ClientPrincipalJsonSerializerContext.Default.ClientPrincipal);
        }
        catch
        {
            return Task.FromResult(AuthenticationResult.Failed(new AuthenticationFailureReason("Invalid x-ms-client-principal value")));
        }

        if (clientPrincipal is null)
        {
            return Task.FromResult(AuthenticationResult.Failed(new AuthenticationFailureReason("Unable to deserialize client principal")));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),
            new(ClaimTypes.Name, userName),
        };

        claims.AddRange(clientPrincipal.UserRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(clientPrincipal.Claims.Select(c => new Claim(c.typ, c.val)));

        var identity = new ClaimsIdentity(claims, "MicrosoftIdentityPlatform");
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticationResult.Succeeded(principal));
    }
}
