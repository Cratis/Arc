// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_HttpRequestContextMiddleware;

public class when_invoking : Specification
{
    HttpRequestContextMiddleware _middleware;
    HttpContext _httpContext;
    IHttpRequestContextAccessor _accessor;
    RequestDelegate _next;
    IHttpRequestContext? _currentWhenNextInvoked;

    void Establish()
    {
        _httpContext = Substitute.For<HttpContext>();
        _accessor = Substitute.For<IHttpRequestContextAccessor>();
        _next = Substitute.For<RequestDelegate>();

        // Capture what the accessor exposes at the moment the next delegate runs, to prove the context is published
        // before the rest of the pipeline — not only by the time the request completes.
        _next.Invoke(Arg.Any<HttpContext>()).Returns(_ =>
        {
            _currentWhenNextInvoked = _accessor.Current;
            return Task.CompletedTask;
        });

        _middleware = new HttpRequestContextMiddleware(_accessor);
    }

    Task Because() => _middleware.InvokeAsync(_httpContext, _next);

    [Fact] void should_publish_the_request_context() => _accessor.Current.ShouldBeOfExactType<AspNetCoreHttpRequestContext>();
    [Fact] void should_publish_it_before_invoking_the_next_delegate() => _currentWhenNextInvoked.ShouldNotBeNull();
    [Fact] void should_invoke_the_next_delegate() => _next.Received(1).Invoke(_httpContext);
}
