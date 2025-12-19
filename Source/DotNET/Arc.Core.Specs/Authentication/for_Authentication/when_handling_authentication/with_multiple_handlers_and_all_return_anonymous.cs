// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication;

public class with_multiple_handlers_and_all_return_anonymous : given.an_authentication_system
{
    IAuthenticationHandler _firstHandler;
    IAuthenticationHandler _secondHandler;
    AuthenticationResult _result;

    void Establish()
    {
        _firstHandler = Substitute.For<IAuthenticationHandler>();
        _secondHandler = Substitute.For<IAuthenticationHandler>();
        _firstHandler.HandleAuthentication(_context).Returns(AuthenticationResult.Anonymous);
        _secondHandler.HandleAuthentication(_context).Returns(AuthenticationResult.Anonymous);
        _handlers.GetEnumerator().Returns(new List<IAuthenticationHandler> { _firstHandler, _secondHandler }.GetEnumerator());
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_return_anonymous_result() => _result.ShouldEqual(AuthenticationResult.Anonymous);
    [Fact] void should_call_both_handlers() => _secondHandler.Received(1).HandleAuthentication(_context);
}
