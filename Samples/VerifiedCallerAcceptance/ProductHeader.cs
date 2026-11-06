// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace VerifiedCallerAcceptance;

/// <summary>
/// A sample-only default scheme for checking same-subject narrowing and different-subject refusal.
/// Never use a caller-supplied header as production authentication.
/// </summary>
/// <param name="options">Scheme options.</param>
/// <param name="logger">Logging factory.</param>
/// <param name="encoder">URL encoder.</param>
public sealed class ProductHeader(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Product-User", out var subject)) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity([new Claim("sub", subject.ToString()), new Claim("scope", "content:write read"), new Claim(ClaimTypes.Role, "editor")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
