// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_HttpRequestContextMiddleware;

/// <summary>
/// A reactor dispatched from an append made during the request carries the request's execution context. Once the
/// pipeline has completed, the request it captured is over and must not be visible to it any more - reading the
/// user of a disposed request throws (Cratis/Arc#3501).
/// </summary>
public class when_work_captured_during_the_request_runs_after_it : Specification
{
    HttpRequestContextMiddleware _middleware;
    HttpRequestContextAccessor _accessor;
    ExecutionContext? _captured;
    IHttpRequestContext? _seenAfterTheRequest;

    void Establish()
    {
        _accessor = new HttpRequestContextAccessor();
        _middleware = new HttpRequestContextMiddleware(_accessor);
    }

    async Task Because()
    {
        await _middleware.InvokeAsync(Substitute.For<HttpContext>(), _ =>
        {
            _captured = ExecutionContext.Capture();
            return Task.CompletedTask;
        });

        ExecutionContext.Run(_captured!, _ => _seenAfterTheRequest = _accessor.Current, null);
    }

    [Fact] void should_see_no_request() => _seenAfterTheRequest.ShouldBeNull();
}
