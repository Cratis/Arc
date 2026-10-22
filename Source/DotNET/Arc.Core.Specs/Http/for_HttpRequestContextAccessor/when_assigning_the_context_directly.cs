// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextAccessor;

/// <summary>
/// Assigning the context is how scoped work such as an observable query emission selects the request it runs
/// under. It keeps doing that - only ending a request clears it.
/// </summary>
public class when_assigning_the_context_directly : Specification
{
    HttpRequestContextAccessor _accessor;
    IHttpRequestContext _context;
    IHttpRequestContext? _seenInACapturedContext;

    void Establish()
    {
        _accessor = new HttpRequestContextAccessor();
        _context = Substitute.For<IHttpRequestContext>();
    }

    void Because()
    {
        ExecutionContext? captured = null;
        ExecutionContext.Run(
            ExecutionContext.Capture()!,
            _ =>
            {
                _accessor.Current = _context;
                captured = ExecutionContext.Capture();
                _accessor.Current = null;
            },
            null);

        ExecutionContext.Run(captured!, _ => _seenInACapturedContext = _accessor.Current, null);
    }

    [Fact] void should_keep_it_for_execution_contexts_captured_while_it_was_assigned() => _seenInACapturedContext.ShouldEqual(_context);
}
