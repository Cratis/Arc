// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.when_generating_from_current_context;

public class and_identity_name_is_missing : given.an_identity_provider_result_handler
{
    IdentityProviderResult _result;
    IdentityDetails _identityDetails;

    void Establish()
    {
        var identity = new ClaimsIdentity("Test");
        identity.AddClaim(new Claim("sub", "user123"));
        identity.AddClaim(new Claim("email", "test@example.com"));
        _claimsPrincipal = new ClaimsPrincipal(identity);
        _httpContext.User = _claimsPrincipal;

        _identityDetails = new IdentityDetails(true, new { Department = "Engineering" });
        _identityProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(_identityDetails);
    }

    async Task Because() => _result = await _handler.GenerateFromCurrentContext();

    [Fact] void should_use_unknown_as_identity_name() => _result.Name.ShouldEqual(new IdentityName("unknown"));
    [Fact] void should_call_identity_provider_with_unknown_name() => _identityProvider.Received(1).Provide(Arg.Is<IdentityProviderContext>(ctx => ctx.Name == "unknown"));
}