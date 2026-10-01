// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_IdentityProvider.when_generating_from_current_context;

public class and_name_claim_is_missing_and_headers_are_not_trusted : given.an_identity_provider_result_handler
{
    IdentityProviderResult _result;

    void Establish()
    {
        _httpRequestContext.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-789")], "OtherScheme")));
        _httpRequestContext.Headers.Returns(new Dictionary<string, string>
        {
            [MicrosoftIdentityPlatformHeaders.IdentityNameHeader] = "Forged Name"
        });
        _identityProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(new IdentityDetails(true, new { }));
    }

    async Task Because() => _result = await _handler.Get();

    [Fact] void should_preserve_authentication_from_the_other_scheme() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_not_copy_the_untrusted_header() => _result.Name.Value.ShouldEqual("unknown");
    [Fact] void should_not_pass_the_untrusted_name_to_the_details_provider() => _identityProvider.Received(1).Provide(Arg.Is<IdentityProviderContext>(context => context.Name.Value == "unknown"));
}
