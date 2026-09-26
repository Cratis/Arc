// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_authentication;

public class and_catalog_requires_authentication : given.a_handler
{
    AuthenticationResult _result;

    async Task Because()
    {
        var context = ContextForPrincipal("workforce");
        context.SetEndpointMetadata(new EndpointMetadata("IntrospectCommands") { RequireAuthentication = true });
        _result = await _handler.HandleAuthentication(context);
    }

    [Fact] void should_not_trust_client_supplied_identity_headers() => _result.IsAuthenticated.ShouldBeFalse();
}
