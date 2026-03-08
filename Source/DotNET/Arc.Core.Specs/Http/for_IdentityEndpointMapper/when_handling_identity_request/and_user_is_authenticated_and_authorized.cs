// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace Cratis.Arc.Http.for_IdentityEndpointMapper.when_handling_identity_request;

public class and_user_is_authenticated_and_authorized : given.an_identity_endpoint_handler
{
    IdentityProviderResult _result;

    void Establish()
    {
        _result = new IdentityProviderResult(
            new IdentityId("test-id"),
            new IdentityName("Test User"),
            IsAuthenticated: true,
            IsAuthorized: true,
            Roles: [],
            "Additional details");

        _identityProviderResultHandler.GenerateFromCurrentContext().Returns(Task.FromResult(_result));
    }

    async Task Because() => await _capturedHandler(_httpRequestContext);

    [Fact] void should_call_generate_from_current_context() => _identityProviderResultHandler.Received(1).GenerateFromCurrentContext();
    [Fact] void should_call_write_with_result() => _identityProviderResultHandler.Received(1).Write(_result);
    [Fact] void should_not_set_status_code_to_unauthorized() => _httpRequestContext.DidNotReceive().StatusCode = 401;
    [Fact] void should_not_set_status_code_to_forbidden() => _httpRequestContext.DidNotReceive().StatusCode = 403;
}
