// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProvider.when_generating_from_current_context;

public class and_a_forged_identity_cookie_disagrees_with_the_principal : given.an_authenticated_user
{
    IdentityProviderResult _result;

    void Establish() => _httpRequestContext.Cookies.Returns(given.forged_identity_cookie.AsCookies(_options));

    async Task Because() => _result = await _handler.Get();

    [Fact] void should_report_the_principal() => _result.Id.Value.ShouldEqual("user-123");
    [Fact] void should_report_the_name_of_the_principal() => _result.Name.Value.ShouldEqual("Test User");
    [Fact] void should_report_the_roles_of_the_principal() => _result.Roles.ShouldContainOnly("Admin");
    [Fact] void should_report_the_details_provided_for_the_principal() => _result.Details.ShouldEqual(_identityDetails.Details);
    [Fact] void should_ask_the_details_provider() => _identityProvider.Received(1).Provide(Arg.Is<IdentityProviderContext>(_ => _.Id.Value == "user-123"));
}
