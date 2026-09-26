// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_authentication;

public class and_forwarded_identity_headers_are_trusted : given.a_handler
{
    AuthenticationResult _result;

    async Task Because()
    {
        var options = Substitute.For<IOptions<ArcOptions>>();
        options.Value.Returns(new ArcOptions { Introspection = new() { TrustForwardedIdentityHeaders = true } });
        var handler = new MicrosoftIdentityPlatformAuthenticationHandler(options, NullLoggerFactory.Instance);
        var context = ContextForPrincipal("workforce");
        context.SetEndpointMetadata(new EndpointMetadata("IntrospectQueries") { RequireAuthentication = true });
        _result = await handler.HandleAuthentication(context);
    }

    [Fact] void should_use_the_trusted_ingress_identity() => _result.IsAuthenticated.ShouldBeTrue();
}
