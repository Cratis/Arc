// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.given;

public class an_authenticated_user : an_identity_provider_result_handler
{
    protected ClaimsPrincipal _claimsPrincipal;
    protected IdentityDetails _identityDetails;

    void Establish()
    {
        var claims = new[]
        {
            new Claim("sub", "user-123"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        _claimsPrincipal = new ClaimsPrincipal(identity);

        _httpRequestContext.User.Returns(_claimsPrincipal);

        _identityDetails = new IdentityDetails(true, new { role = "Admin" });
        _identityProvider.Provide(Arg.Any<IdentityProviderContext>()).Returns(_identityDetails);
    }
}
