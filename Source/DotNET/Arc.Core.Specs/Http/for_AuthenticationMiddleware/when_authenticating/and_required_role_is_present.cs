// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http.for_AuthenticationMiddleware.when_authenticating;

public class and_required_role_is_present : given.an_authentication_middleware
{
    bool _result;

    void Establish()
    {
        _metadata = new EndpointMetadata("Catalog") { RequireAuthentication = true, Roles = "Administrator,Operator" };
        _authentication.HandleAuthentication(_httpRequestContext).Returns(AuthenticationResult.Succeeded(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Operator")], "TestScheme"))));
    }

    async Task Because() => _result = await _middleware.Authenticate(_httpRequestContext, _metadata);

    [Fact] void should_allow_request() => _result.ShouldBeTrue();
}
