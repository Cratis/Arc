// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProviderEndpoint.when_handling;

public class and_user_is_not_authorized : given.a_valid_identity_request
{
    void Establish()
    {
        _identityProviderResult = new IdentityProviderResult(new IdentityId("123"), new IdentityName("Test User"), true, false, "Not authorized");
        _identityProviderResultHandler.GenerateFromCurrentContext().Returns(_identityProviderResult);
    }

    Task Because() => _endpoint.Handler(_response);

    [Fact] void should_call_generate_from_current_context() => _identityProviderResultHandler.Received(1).GenerateFromCurrentContext();
    [Fact] void should_not_call_write() => _identityProviderResultHandler.DidNotReceive().Write(Arg.Any<IdentityProviderResult>());
    [Fact] void should_set_status_code_to_403() => _response.StatusCode.ShouldEqual(403);
}