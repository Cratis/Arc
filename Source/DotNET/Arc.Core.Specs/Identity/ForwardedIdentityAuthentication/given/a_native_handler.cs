// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_ForwardedIdentityAuthentication.given;

public class a_native_handler : IAuthenticationHandler
{
    public bool AppliesToAnonymousEndpoints => true;

    public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context) => Task.FromResult(AuthenticationResult.Succeeded(
        new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "native")], "native"))));
}
