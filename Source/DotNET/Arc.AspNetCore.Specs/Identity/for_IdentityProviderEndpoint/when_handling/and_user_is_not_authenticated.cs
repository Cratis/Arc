// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProviderEndpoint.when_handling;

public class and_user_is_not_authenticated : given.an_identity_provider_endpoint
{
    void Establish()
    {
        _identityProviderResultHandler.GenerateFromCurrentContext().Returns(IdentityProviderResult.Anonymous);
    }

    Task Because() => _endpoint.Handler(_response);

    [Fact] void should_call_generate_from_current_context() => _identityProviderResultHandler.Received(1).GenerateFromCurrentContext();
    [Fact] void should_not_call_write() => _identityProviderResultHandler.DidNotReceive().Write(Arg.Any<IdentityProviderResult>());
    [Fact] void should_set_status_code_to_401() => _response.StatusCode.ShouldEqual(401);
}
