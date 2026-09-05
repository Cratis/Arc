// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_AspNetCoreHttpRequestContext.when_reading_headers;

/// <summary>
/// HTTP/2 lowercases header names on the wire, so a header configured as <c>Tenant-ID</c> arrives as
/// <c>tenant-id</c>. The lookup must still find it, otherwise header-based tenant resolution silently yields nothing.
/// </summary>
public class with_a_differently_cased_header : Specification
{
    AspNetCoreHttpRequestContext _context;
    bool _found;
    string? _value;

    void Establish()
    {
        var headers = new HeaderDictionary { { "tenant-id", "acme" } };
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        httpContext.Request.Returns(request);
        request.Headers.Returns(headers);
        _context = new AspNetCoreHttpRequestContext(httpContext);
    }

    void Because() => _found = _context.Headers.TryGetValue("Tenant-ID", out _value);

    [Fact] void should_find_the_header_ignoring_case() => _found.ShouldBeTrue();
    [Fact] void should_return_the_header_value() => _value.ShouldEqual("acme");
}
