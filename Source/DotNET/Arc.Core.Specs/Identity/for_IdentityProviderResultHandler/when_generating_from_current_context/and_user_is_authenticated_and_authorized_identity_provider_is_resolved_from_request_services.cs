// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_generating_from_current_context;

public class and_user_is_authenticated_and_authorized_identity_provider_is_resolved_from_request_services : given.an_authenticated_user
{
    IdentityProviderResult _result;

    async Task Because() => _result = await _handler.GenerateFromCurrentContext();

    [Fact] void should_resolve_identity_provider_from_request_services() => _requestServices.Received(1).GetService(typeof(IProvideIdentityDetails));
    [Fact] void should_return_authenticated_result() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_return_authorized_result() => _result.IsAuthorized.ShouldBeTrue();
}
