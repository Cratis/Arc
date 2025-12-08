// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_generating_from_current_context;

public class and_user_is_authenticated_but_not_authorized : given.an_identity_provider_result_handler
{
    IdentityProviderResult _result;
    IdentityDetails _identityDetails;

    void Establish()
    {
        var claims = new[]
        {
            new Claim("sub", "user123"),
            new Claim("name", "Test User"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        _httpContext.User = new ClaimsPrincipal(identity);

        _identityDetails = new IdentityDetails(false, new { Department = "Engineering" });
        _identityProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(_identityDetails);
    }

    async Task Because() => _result = await _handler.GenerateFromCurrentContext();

    [Fact] void should_return_unauthorized_result() => _result.ShouldEqual(IdentityProviderResult.Unauthorized);
    [Fact] void should_call_identity_provider_with_correct_context() => _identityProvider.Received(1).Provide(Arg.Is<IdentityProviderContext>(ctx =>
        ctx.Id == "user123" &&
        ctx.Name == "Test User" &&
        ctx.Claims.Any(c => c.Key == "sub" && c.Value == "user123")));
}