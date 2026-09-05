// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_IdentityProvider.when_generating_from_current_context;

public class and_name_claim_is_missing_but_header_is_present : given.an_identity_provider_result_handler
{
    IdentityProviderResult _result;

    void Establish()
    {
        var claims = new[]
        {
            new Claim("sub", "user-789")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _httpRequestContext.User.Returns(claimsPrincipal);
        _httpRequestContext.Headers.Returns(new Dictionary<string, string>
        {
            [MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "Header User"
        });

        var identityDetails = new IdentityDetails(true, new { });
        _identityProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(identityDetails);
    }

    async Task Because() => _result = await _handler.Get();

    [Fact] void should_return_authenticated_result() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_return_authorized_result() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_use_name_from_header() => _result.Name.Value.ShouldEqual("Header User");
}
