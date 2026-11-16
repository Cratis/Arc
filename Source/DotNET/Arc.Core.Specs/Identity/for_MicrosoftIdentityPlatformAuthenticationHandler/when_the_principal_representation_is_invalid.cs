// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.for_MicrosoftIdentityPlatformAuthenticationHandler;

public class when_the_principal_representation_is_invalid : given.a_handler
{
    [Theory]
    [InlineData("")]
    [InlineData("not-base64")]
    [InlineData("ew==")]
    [InlineData("bnVsbA==")]
    public async Task should_fail_authentication_instead_of_treating_the_request_as_anonymous(string principal)
    {
        var context = ContextForPrincipal("workforce");
        var headers = context.Headers.ToDictionary();
        headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader] = principal;
        context.Headers.Returns(headers);

        var result = await _handler.HandleAuthentication(context);

        result.IsAuthenticated.ShouldBeFalse();
        result.Failure!.Reason.Value.ShouldEqual("Not authenticated - invalid representation of ClientPrincipal");
    }
}
