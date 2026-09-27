// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextExtensions.when_setting_no_store_response_headers;

public class with_a_context_without_response_header_reader : Specification
{
    readonly Dictionary<string, string> _responseHeaders = new(StringComparer.OrdinalIgnoreCase);
    IHttpRequestContext _context;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _context
            .When(_ => _.SetResponseHeader(Arg.Any<string>(), Arg.Any<string>()))
            .Do(call => _responseHeaders[call.ArgAt<string>(0)] = call.ArgAt<string>(1));
    }

    void Because() => _context.SetNoStoreResponseHeaders();

    [Fact] void should_assign_vary_cookie() => _responseHeaders["Vary"].ShouldEqual("Cookie");
}
