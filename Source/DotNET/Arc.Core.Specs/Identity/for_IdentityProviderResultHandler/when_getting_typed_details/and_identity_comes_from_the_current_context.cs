// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProvider.when_getting_typed_details;

public class and_identity_comes_from_the_current_context : given.an_identity_provider_result_handler
{
    IdentityProviderResult<given.ProfileDetails> _result;

    void Establish()
    {
        // Details of a different runtime type convert into the requested type through JSON.
        var details = new
        {
            given.representative_identity.Details.Department,
            given.representative_identity.Details.Level,
            given.representative_identity.Details.Access,
            given.representative_identity.Details.Address,
            given.representative_identity.Details.Groups
        };
        _identityProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(new IdentityDetails(true, details));
        var user = CreateAuthenticatedUser();
        _httpRequestContext.User.Returns(user);
    }

    async Task Because() => _result = await _handler.Get<given.ProfileDetails>();

    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_convert_the_department() => _result.Details.Department.ShouldEqual(given.representative_identity.Details.Department);
    [Fact] void should_convert_the_level_concept() => _result.Details.Level.ShouldEqual(given.representative_identity.Details.Level);
    [Fact] void should_convert_the_enum() => _result.Details.Access.ShouldEqual(given.AccessKind.Administrator);
    [Fact] void should_convert_the_nested_object() => _result.Details.Address.ShouldEqual(given.representative_identity.Details.Address);
    [Fact] void should_leave_the_missing_nickname_null() => _result.Details.Nickname.ShouldBeNull();
}
