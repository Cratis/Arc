// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication;

public class with_single_handler_that_returns_anonymous : given.an_authentication_system
{
    IAuthenticationHandler _handler;
    AuthenticationResult _result;

    void Establish()
    {
        _handler = Substitute.For<IAuthenticationHandler>();
        _handler.HandleAuthentication(_context).Returns(AuthenticationResult.Anonymous);
        _handlers.GetEnumerator().Returns(new List<IAuthenticationHandler> { _handler }.GetEnumerator());
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_return_anonymous_result() => _result.ShouldEqual(AuthenticationResult.Anonymous);
}
