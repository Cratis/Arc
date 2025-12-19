// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication;

public class with_multiple_handlers_and_first_authenticates : given.an_authentication_system
{
    IAuthenticationHandler _firstHandler;
    IAuthenticationHandler _secondHandler;
    ClaimsPrincipal _principal;
    AuthenticationResult _result;

    void Establish()
    {
        _firstHandler = Substitute.For<IAuthenticationHandler>();
        _secondHandler = Substitute.For<IAuthenticationHandler>();
        _principal = new ClaimsPrincipal();
        _firstHandler.HandleAuthentication(_context).Returns(AuthenticationResult.Succeeded(_principal));
        _handlers.GetEnumerator().Returns(new List<IAuthenticationHandler> { _firstHandler, _secondHandler }.GetEnumerator());
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_return_authenticated_result() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_return_principal_from_first_handler() => _result.Principal.ShouldEqual(_principal);
    [Fact] void should_not_call_second_handler() => _secondHandler.DidNotReceive().HandleAuthentication(_context);
}
