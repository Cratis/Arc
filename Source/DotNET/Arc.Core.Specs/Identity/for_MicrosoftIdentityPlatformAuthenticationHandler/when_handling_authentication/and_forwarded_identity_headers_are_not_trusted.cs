// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler.when_handling_authentication;

public class and_forwarded_identity_headers_are_not_trusted : given.a_handler
{
    AuthenticationResult _result;

    async Task Because()
    {
        var handler = new MicrosoftIdentityPlatformAuthenticationHandler(Options.Create(new ArcOptions()), NullLoggerFactory.Instance);
        _result = await handler.HandleAuthentication(ContextForPrincipal("workforce"));
    }

    [Fact] void should_not_authenticate_the_request() => _result.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_treat_the_request_as_anonymous() => _result.ShouldEqual(AuthenticationResult.Anonymous);
}
