// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions.given;

public class normalizing_wrapper_handler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var result = await Context.AuthenticateAsync("Headers");
        if (!result.Succeeded)
        {
            return AuthenticateResult.NoResult();
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(result.Principal.Claims, "Normalized"));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
