// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_generating_from_current_context;

public class and_user_is_authenticated_and_authorized : given.an_authenticated_user
{
    IdentityProviderResult _result;

    async Task Because() => _result = await _handler.GenerateFromCurrentContext();

    [Fact] void should_return_authenticated_result() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_return_authorized_result() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_have_user_id() => _result.Id.Value.ShouldEqual("user-123");
    [Fact] void should_have_user_name() => _result.Name.Value.ShouldEqual("Test User");
    [Fact] void should_have_details() => _result.Details.ShouldNotBeNull();
    [Fact] void should_call_identity_provider() => _identityProvider.Received(1).Provide(Arg.Any<IdentityProviderContext>());
}
