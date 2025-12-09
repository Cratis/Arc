// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_generating_from_current_context;

public class and_user_is_not_authorized : given.an_identity_provider_result_handler
{
    IdentityProviderResult _result;

    void Establish()
    {
        var claims = new[]
        {
            new Claim("sub", "user-456"),
            new Claim(ClaimTypes.Name, "Unauthorized User")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _httpRequestContext.User.Returns(claimsPrincipal);

        var identityDetails = new IdentityDetails(false, new { role = "None" });
        _identityProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(identityDetails);
    }

    async Task Because() => _result = await _handler.GenerateFromCurrentContext();

    [Fact] void should_return_unauthorized_result() => _result.ShouldEqual(IdentityProviderResult.Unauthorized);
    [Fact] void should_call_identity_provider() => _identityProvider.Received(1).Provide(Arg.Any<IdentityProviderContext>());
}
