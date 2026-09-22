// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http.for_AuthenticationMiddleware.when_authenticating;

public class and_a_handler_opts_out_of_anonymous_endpoints : Specification
{
    OptedOutHandler _handler;
    AuthenticationMiddleware _middleware;
    IHttpRequestContext _httpRequestContext;
    EndpointMetadata _metadata;
    bool _result;

    void Establish()
    {
        _handler = new OptedOutHandler();

        var handlers = Substitute.For<IInstancesOf<IAuthenticationHandler>>();
        handlers.GetEnumerator().Returns(new List<IAuthenticationHandler> { _handler }.GetEnumerator());

        var authentication = new Cratis.Arc.Authentication.Authentication(handlers);
        _middleware = new AuthenticationMiddleware(authentication);
        _httpRequestContext = Substitute.For<IHttpRequestContext>();
        _httpRequestContext.Items.Returns(new Dictionary<object, object?>());
        _metadata = new EndpointMetadata("TestEndpoint", AllowAnonymous: true);
    }

    async Task Because() => _result = await _middleware.Authenticate(_httpRequestContext, _metadata);

    [Fact] void should_allow_the_request_through() => _result.ShouldBeTrue();
    [Fact] void should_not_call_the_opted_out_handler() => _handler.WasCalled.ShouldBeFalse();
    [Fact] void should_not_set_status_code() => _httpRequestContext.DidNotReceive().SetStatusCode(Arg.Any<int>());

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
