// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler;

public class when_required_headers_are_missing : given.a_handler
{
    [Theory]
    [InlineData(MicrosoftIdentityPlatformHeaders.IdentityIdHeader)]
    [InlineData(MicrosoftIdentityPlatformHeaders.IdentityNameHeader)]
    [InlineData(MicrosoftIdentityPlatformHeaders.PrincipalHeader)]
    public async Task should_treat_the_request_as_anonymous(string missingHeader)
    {
        var context = ContextForPrincipal("workforce");
        context.Headers.Returns(context.Headers.Where(header => header.Key != missingHeader).ToDictionary());

        var result = await _handler.HandleAuthentication(context);

        result.ShouldEqual(AuthenticationResult.Anonymous);
    }
}
