// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Authentication.for_Authentication.when_handling_authentication.and_endpoint_allows_anonymous;

public class with_handlers_and_endpoint_metadata
{
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(null, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(null, false)]
    public async Task should_only_skip_an_explicit_opt_out_on_an_anonymous_endpoint(bool? allowsAnonymous, bool usesDefault)
    {
        var wasCalled = false;
        IAuthenticationHandler handler = usesDefault ? new DefaultHandler(() => wasCalled = true) : new OptedOutHandler(() => wasCalled = true);
        var handlers = Substitute.For<IInstancesOf<IAuthenticationHandler>>();
        handlers.GetEnumerator().Returns(_ => new List<IAuthenticationHandler> { handler }.GetEnumerator());
        var context = Substitute.For<IHttpRequestContext>();
        context.Items.Returns(new Dictionary<object, object?>());
        if (allowsAnonymous.HasValue) context.SetEndpointMetadata(new EndpointMetadata("Endpoint", AllowAnonymous: allowsAnonymous.Value));

        var result = await new Authentication(handlers).HandleAuthentication(context);

        wasCalled.ShouldEqual(usesDefault || allowsAnonymous != true);
        if (usesDefault || allowsAnonymous != true) result.Failure.ShouldNotBeNull();
        else result.ShouldEqual(AuthenticationResult.Anonymous);
    }

    class DefaultHandler(Action called) : IAuthenticationHandler
    {
        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
        {
            called();
            return Task.FromResult(AuthenticationResult.Failed(new AuthenticationFailureReason("MissingApiKey")));
        }
    }

    class OptedOutHandler(Action called) : IAuthenticationHandler
    {
        public bool AppliesToAnonymousEndpoints => false;

        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context)
        {
            called();
            return Task.FromResult(AuthenticationResult.Failed(new AuthenticationFailureReason("MissingApiKey")));
        }
    }
}
