// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication;

public class with_single_handler_that_fails : given.an_authentication_system
{
    IAuthenticationHandler _handler;
    AuthenticationFailure _failure;
    AuthenticationResult _result;

    void Establish()
    {
        _handler = Substitute.For<IAuthenticationHandler>();
        _failure = new AuthenticationFailure(new AuthenticationFailureReason("Invalid credentials"));
        _handler.HandleAuthentication(_context).Returns(AuthenticationResult.Failed(new AuthenticationFailureReason("Invalid credentials")));
        _handlers.GetEnumerator().Returns(new List<IAuthenticationHandler> { _handler }.GetEnumerator());
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_return_unauthenticated_result() => _result.IsAuthenticated.ShouldBeFalse();
    [Fact] void should_return_failure() => _result.Failure.ShouldNotBeNull();
    [Fact] void should_return_failure_with_invalid_credentials_reason() => _result.Failure!.Reason.Value.ShouldEqual("Invalid credentials");
}
