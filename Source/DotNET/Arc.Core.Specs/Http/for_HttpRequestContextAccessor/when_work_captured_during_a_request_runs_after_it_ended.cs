// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextAccessor;

/// <summary>
/// Work that captures the execution context during a request - a reactor dispatched from an append, a background
/// task - can run after the request has ended. Reading the request then has to find none, not a disposed request
/// whose user throws on access (Cratis/Arc#3501).
/// </summary>
public class when_work_captured_during_a_request_runs_after_it_ended : Specification
{
    HttpRequestContextAccessor _accessor;
    IHttpRequestContext _context;
    ExecutionContext _captured;
    IHttpRequestContext? _seenDuringTheRequest;
    IHttpRequestContext? _seenAfterTheRequest;

    void Establish()
    {
        _accessor = new HttpRequestContextAccessor();
        _context = Substitute.For<IHttpRequestContext>();
    }

    void Because()
    {
        ExecutionContext.Run(
            ExecutionContext.Capture()!,
            _ =>
            {
                using (HttpRequestContextAccessor.BeginRequest(_accessor, _context))
                {
                    _captured = ExecutionContext.Capture()!;
                    _seenDuringTheRequest = _accessor.Current;
                }
            },
            null);

        ExecutionContext.Run(_captured, _ => _seenAfterTheRequest = _accessor.Current, null);
    }

    [Fact] void should_see_the_request_while_it_is_in_progress() => _seenDuringTheRequest.ShouldEqual(_context);
    [Fact] void should_see_no_request_after_it_ended() => _seenAfterTheRequest.ShouldBeNull();
}
