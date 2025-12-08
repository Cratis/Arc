// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_generating_from_current_context;

public class and_user_is_not_authenticated : given.an_identity_provider_result_handler
{
    IdentityProviderResult _result;

    void Establish()
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("sub", "user123"));
        identity.AddClaim(new Claim("name", "Test User"));
        _claimsPrincipal = new ClaimsPrincipal(identity);
        _httpContext.User = _claimsPrincipal;
    }

    async Task Because() => _result = await _handler.GenerateFromCurrentContext();

    [Fact] void should_return_anonymous_result() => _result.ShouldEqual(IdentityProviderResult.Anonymous);
}