// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication;

public class with_multiple_handlers_and_first_fails : given.an_authentication_system
{
    IAuthenticationHandler _firstHandler;
    IAuthenticationHandler _secondHandler;
    AuthenticationResult _result;

    void Establish()
    {
        _firstHandler = Substitute.For<IAuthenticationHandler>();
        _secondHandler = Substitute.For<IAuthenticationHandler>();
        _firstHandler.HandleAuthentication(_context).Returns(AuthenticationResult.Failed(new AuthenticationFailureReason("Invalid credentials")));
        _handlers.GetEnumerator().Returns(new List<IAuthenticationHandler> { _firstHandler, _secondHandler }.GetEnumerator());
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_return_unauthenticated_result() => _result.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_return_failure() => _result.Failure.ShouldNotBeNull();
    [Fact] void should_not_call_second_handler() => _secondHandler.DidNotReceive().HandleAuthentication(_context);
}
