// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpRequestContextExtensions.when_setting_no_store_response_headers.given;

public class a_context_tracking_response_headers : Specification
{
    protected IHttpRequestContext _context;
    protected Dictionary<string, string> _responseHeaders;

    void Establish()
    {
        _responseHeaders = new(StringComparer.OrdinalIgnoreCase);
        _context = Substitute.For<IHttpRequestContext>();
        _context.GetResponseHeader(Arg.Any<string>()).Returns(call => _responseHeaders.TryGetValue(call.Arg<string>(), out var value) ? value : null);
        _context
            .When(_ => _.SetResponseHeader(Arg.Any<string>(), Arg.Any<string>()))
            .Do(call => _responseHeaders[call.ArgAt<string>(0)] = call.ArgAt<string>(1));
    }
}
