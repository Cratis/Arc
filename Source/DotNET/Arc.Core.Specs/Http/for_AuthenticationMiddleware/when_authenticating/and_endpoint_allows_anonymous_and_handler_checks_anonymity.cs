// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

using AuthenticationService = Cratis.Arc.Authentication.Authentication;

namespace Cratis.Arc.Http.for_AuthenticationMiddleware.when_authenticating;

/// <summary>
/// Regression spec for a handler that would otherwise reject the request but opts out by checking
/// <see cref="HttpRequestContextEndpointExtensions.AllowsAnonymous"/> - mirrors an API-key handler that logs a
/// rejection when credentials are missing, reached through the real <see cref="Authentication"/> service rather
/// than a substitute, so the metadata is genuinely visible to the handler when it runs.
/// </summary>
public class and_endpoint_allows_anonymous_and_handler_checks_anonymity : Specification
{
    IInstancesOf<IAuthenticationHandler> _handlers;
    HandlerThatStaysQuietOnAnonymousEndpoints _handler;
    AuthenticationMiddleware _middleware;
    IHttpRequestContext _httpRequestContext;
    EndpointMetadata _metadata;
    bool _result;

    void Establish()
    {
        _handler = new HandlerThatStaysQuietOnAnonymousEndpoints();
        _handlers = Substitute.For<IInstancesOf<IAuthenticationHandler>>();
        _handlers.GetEnumerator().Returns(_ => new List<IAuthenticationHandler> { _handler }.GetEnumerator());
        _middleware = new AuthenticationMiddleware(new AuthenticationService(_handlers));
        _httpRequestContext = Substitute.For<IHttpRequestContext>();
        _httpRequestContext.Items.Returns(new Dictionary<object, object?>());
        _metadata = new EndpointMetadata("Health", AllowAnonymous: true);
    }

    async Task Because() => _result = await _middleware.Authenticate(_httpRequestContext, _metadata);

    [Fact] void should_allow_the_request_through() => _result.ShouldBeTrue();
    [Fact] void should_not_set_status_code() => _httpRequestContext.DidNotReceive().SetStatusCode(Arg.Any<int>());
    [Fact] void should_not_ask_the_handler_to_reject() => _handler.WasAskedToReject.ShouldBeFalse();

    class HandlerThatStaysQuietOnAnonymousEndpoints : IAuthenticationHandler
    {
        public bool WasAskedToReject { get; private set; }

        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
        {
            if (context.AllowsAnonymous())
            {
                return Task.FromResult(AuthenticationResult.Anonymous);
            }

            WasAskedToReject = true;
            return Task.FromResult(AuthenticationResult.Failed(new AuthenticationFailureReason("Missing credentials")));
        }
    }
}
