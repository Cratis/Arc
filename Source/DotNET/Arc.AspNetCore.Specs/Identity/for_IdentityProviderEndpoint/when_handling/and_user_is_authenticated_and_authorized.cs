// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProviderEndpoint.when_handling;

public class and_user_is_authenticated_and_authorized : given.a_valid_identity_request
{
    Task Because() => _endpoint.Handler(_response);

    [Fact] void should_call_generate_from_current_context() => _identityProviderResultHandler.Received(1).GenerateFromCurrentContext();
    [Fact] void should_call_write_with_result() => _identityProviderResultHandler.Received(1).Write(_identityProviderResult);
    [Fact] void should_not_set_status_code() => _response.StatusCode.ShouldEqual(200);
}
