// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProvider.when_getting_typed_details;

public class and_a_forged_identity_cookie_is_sent_without_a_principal : given.an_identity_provider_result_handler
{
    IdentityProviderResult<given.ProfileDetails> _result;

    void Establish()
    {
        _httpRequestContext.Cookies.Returns(given.forged_identity_cookie.AsCookies(_options));
        _httpRequestContext.User.Returns((System.Security.Claims.ClaimsPrincipal?)null);
    }

    async Task Because() => _result = await _handler.Get<given.ProfileDetails>();

    [Fact] void should_not_be_authenticated() => _result.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_not_report_the_forged_identity() => _result.Id.Value.ShouldNotEqual(given.forged_identity_cookie.Claimed.Id.Value);
    [Fact] void should_not_ask_the_details_provider() => _identityProvider.DidNotReceive().Provide(Arg.Any<IdentityProviderContext>());
}
