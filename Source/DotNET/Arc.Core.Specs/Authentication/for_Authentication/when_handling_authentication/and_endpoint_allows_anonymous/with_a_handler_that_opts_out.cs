// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication.and_endpoint_allows_anonymous;

public class with_a_handler_that_opts_out : given.an_anonymous_endpoint
{
    OptedOutHandler _handler;
    AuthenticationResult _result;

    void Establish()
    {
        _handler = new OptedOutHandler();
        _handlers.GetEnumerator().Returns(new List<IAuthenticationHandler> { _handler }.GetEnumerator());
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_not_call_the_handler() => _handler.WasCalled.ShouldBeFalse();
    [Fact] void should_return_anonymous_result() => _result.ShouldEqual(AuthenticationResult.Anonymous);

    class OptedOutHandler : IAuthenticationHandler
    {
        public bool WasCalled { get; private set; }

        public bool AppliesToAnonymousEndpoints => false;

        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
        {
            WasCalled = true;
            return Task.FromResult(AuthenticationResult.Failed(new AuthenticationFailureReason("MissingApiKey")));
        }
    }
}
