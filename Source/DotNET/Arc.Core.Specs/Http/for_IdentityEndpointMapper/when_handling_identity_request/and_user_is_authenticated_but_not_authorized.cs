// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_identity_request;

public class and_user_is_authenticated_but_not_authorized : given.an_identity_endpoint_handler
{
    IdentityProviderResult _result;

    void Establish()
    {
        _result = new IdentityProviderResult(
            new IdentityId("test-id"),
            new IdentityName("Test User"),
            IsAuthenticated: true,
            IsAuthorized: false,
            Roles: [],
            string.Empty);

        _identityProviderResultHandler.GenerateFromCurrentContext().Returns(Task.FromResult(_result));
    }

    async Task Because() => await _capturedHandler(_httpRequestContext);

    [Fact] void should_call_generate_from_current_context() => _identityProviderResultHandler.Received(1).GenerateFromCurrentContext();
    [Fact] void should_not_call_write() => _identityProviderResultHandler.DidNotReceive().Write(Arg.Any<IdentityProviderResult>());
    [Fact] void should_set_status_code_to_forbidden() => _httpRequestContext.Received().StatusCode = 403;
}
