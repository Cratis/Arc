// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProvider.when_getting_typed_details;

public class and_a_forged_identity_cookie_disagrees_with_the_principal : given.an_identity_provider_result_handler
{
    IdentityProviderResult<given.ProfileDetails> _result;

    void Establish()
    {
        _identityProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(new IdentityDetails(true, given.representative_identity.Details));
        var user = CreateAuthenticatedUser();
        _httpRequestContext.User.Returns(user);
        _httpRequestContext.Cookies.Returns(given.forged_identity_cookie.AsCookies(_options));
    }

    async Task Because() => _result = await _handler.Get<given.ProfileDetails>();

    [Fact] void should_report_the_principal() => _result.Id.Value.ShouldEqual("user123");
    [Fact] void should_not_report_the_roles_from_the_cookie() => _result.Roles.ShouldBeEmpty();
    [Fact] void should_report_the_details_provided_for_the_principal() => _result.Details.ShouldEqual(given.representative_identity.Details);
}
