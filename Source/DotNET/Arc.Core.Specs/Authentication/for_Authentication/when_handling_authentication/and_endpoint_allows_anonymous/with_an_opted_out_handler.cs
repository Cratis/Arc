// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication.and_endpoint_allows_anonymous;

public class with_an_opted_out_handler : Specification
{
    OptedOutHandler _handler;
    DefaultHandler _next;
    Authentication _authentication;
    IHttpRequestContext _context;
    AuthenticationResult _result;

    void Establish()
    {
        _handler = new OptedOutHandler();
        _next = new DefaultHandler();
        var handlers = Substitute.For<IInstancesOf<IAuthenticationHandler>>();
        handlers.GetEnumerator().Returns(_ => new List<IAuthenticationHandler> { _handler, _next }.GetEnumerator());
        _authentication = new Authentication(handlers);
        _context = Substitute.For<IHttpRequestContext>();
        _context.Items.Returns(new Dictionary<object, object?>());
        _context.SetEndpointMetadata(new EndpointMetadata("Public", AllowAnonymous: true));
    }

    async Task Because() => _result = await _authentication.HandleAuthentication(_context);

    [Fact] void should_skip_the_opted_out_handler() => _handler.WasCalled.ShouldBeFalse();
    [Fact] void should_continue_to_the_next_handler() => _result.IsAuthenticated.ShouldBeTrue();
    [Fact] void should_call_the_next_handler() => _next.WasCalled.ShouldBeTrue();

    class DefaultHandler : IAuthenticationHandler
    {
        public bool WasCalled { get; private set; }

        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
        {
            WasCalled = true;
            return Task.FromResult(AuthenticationResult.Succeeded(new System.Security.Claims.ClaimsPrincipal()));
        }
    }

    class OptedOutHandler : IAuthenticationHandler
    {
        public bool AppliesToAnonymousEndpoints => false;
        public bool WasCalled { get; private set; }

        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
        {
            WasCalled = true;
            return Task.FromResult(AuthenticationResult.Failed(new AuthenticationFailureReason("MissingApiKey")));
        }
    }
}
