// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextAccessor;

public class when_beginning_a_request_on_another_accessor : Specification
{
    IHttpRequestContextAccessor _accessor;
    IHttpRequestContext _context;

    void Establish()
    {
        _accessor = Substitute.For<IHttpRequestContextAccessor>();
        _context = Substitute.For<IHttpRequestContext>();
    }

    void Because() => HttpRequestContextAccessor.BeginRequest(_accessor, _context).Dispose();

    [Fact] void should_assign_the_context_to_it() => _accessor.Received(1).Current = _context;
}
