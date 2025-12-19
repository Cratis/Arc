// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http.for_AuthenticationMiddleware.when_authenticating;

public class and_user_is_authenticated : given.an_authentication_middleware
{
    bool _result;
    ClaimsPrincipal _principal;

    void Establish()
    {
        _metadata = new EndpointMetadata("TestEndpoint", "Test Endpoint", [], AllowAnonymous: false);
        _principal = new ClaimsPrincipal(new ClaimsIdentity("TestScheme"));
        _authentication.HandleAuthentication(_httpRequestContext).Returns(Task.FromResult(AuthenticationResult.Succeeded(_principal)));
    }

    async Task Because() => _result = await _middleware.AuthenticateAsync(_httpRequestContext, _metadata);

    [Fact] void should_return_true() => _result.ShouldBeTrue();
    [Fact] void should_call_authentication() => _authentication.Received(1).HandleAuthentication(_httpRequestContext);
    [Fact] void should_not_set_status_code() => _httpRequestContext.DidNotReceive().SetStatusCode(Arg.Any<int>());
}
