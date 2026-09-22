// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication.and_endpoint_allows_anonymous;

public class with_a_handler_that_applies : given.an_anonymous_endpoint
{
    ApplyingHandler _handler;
    ClaimsPrincipal _principal;
    AuthenticationResult _result;

    void Establish()
    {
        _principal = new ClaimsPrincipal();
        _handler = new ApplyingHandler(_principal);
        _handlers.GetEnumerator().Returns(new List<IAuthenticationHandler> { _handler }.GetEnumerator());
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_call_the_handler() => _handler.WasCalled.ShouldBeTrue();
    [Fact] void should_return_the_handlers_principal() => _result.Principal.ShouldEqual(_principal);

    class ApplyingHandler(ClaimsPrincipal principal) : IAuthenticationHandler
    {
        public bool WasCalled { get; private set; }

        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
        {
            WasCalled = true;
            return Task.FromResult(AuthenticationResult.Succeeded(principal));
        }
    }
}
