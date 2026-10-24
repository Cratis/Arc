// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http.for_AuthenticationMiddleware.when_authenticating;

public class and_a_handler_opts_out_of_anonymous_endpoints : Specification
{
    OptedOutHandler _handler;
    AuthenticationMiddleware _middleware;
    IHttpRequestContext _context;
    bool _result;

    void Establish()
    {
        _handler = new OptedOutHandler();
        var handlers = Substitute.For<IInstancesOf<IAuthenticationHandler>>();
        handlers.GetEnumerator().Returns(_ => new List<IAuthenticationHandler> { _handler }.GetEnumerator());
        _middleware = new AuthenticationMiddleware(new Cratis.Arc.Authentication.Authentication(handlers));
        _context = Substitute.For<IHttpRequestContext>();
        _context.Items.Returns(new Dictionary<object, object?>());
    }

    async Task Because() => _result = await _middleware.Authenticate(_context, new EndpointMetadata("Public", AllowAnonymous: true));

    [Fact] void should_allow_the_request() => _result.ShouldBeTrue();
    [Fact] void should_not_call_the_handler() => _handler.WasCalled.ShouldBeFalse();
    [Fact] void should_not_set_a_status_code() => _context.DidNotReceive().SetStatusCode(Arg.Any<int>());

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
