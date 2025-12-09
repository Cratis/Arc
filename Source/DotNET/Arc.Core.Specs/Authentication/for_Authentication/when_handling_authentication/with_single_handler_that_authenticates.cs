// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication;

public class with_single_handler_that_authenticates : given.an_authentication_system
{
    IAuthenticationHandler _handler;
    ClaimsPrincipal _principal;
    AuthenticationResult _result;

    void Establish()
    {
        _handler = Substitute.For<IAuthenticationHandler>();
        _principal = new ClaimsPrincipal();
        _handler.HandleAuthentication(_context).Returns(AuthenticationResult.Succeeded(_principal));
        _handlers.GetEnumerator().Returns(new List<IAuthenticationHandler> { _handler }.GetEnumerator());
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_return_authenticated_result() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_return_principal_from_handler() => _result.Principal.ShouldEqual(_principal);
}
