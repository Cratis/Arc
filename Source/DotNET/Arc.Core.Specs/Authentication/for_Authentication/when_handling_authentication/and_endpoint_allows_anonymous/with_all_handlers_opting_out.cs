// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication.and_endpoint_allows_anonymous;

public class with_all_handlers_opting_out : given.an_anonymous_endpoint
{
    OptedOutHandler _firstHandler;
    OptedOutHandler _secondHandler;
    AuthenticationResult _result;

    void Establish()
    {
        _firstHandler = new OptedOutHandler();
        _secondHandler = new OptedOutHandler();
        _handlers.GetEnumerator().Returns(new List<IAuthenticationHandler> { _firstHandler, _secondHandler }.GetEnumerator());
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_return_anonymous_result() => _result.ShouldEqual(AuthenticationResult.Anonymous);
    [Fact] void should_not_call_the_first_handler() => _firstHandler.WasCalled.ShouldBeFalse();
    [Fact] void should_not_call_the_second_handler() => _secondHandler.WasCalled.ShouldBeFalse();

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
