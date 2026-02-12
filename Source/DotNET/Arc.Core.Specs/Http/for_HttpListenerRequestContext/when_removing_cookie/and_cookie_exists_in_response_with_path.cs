// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_removing_cookie;

public class and_cookie_exists_in_response_with_path : given.an_http_listener_request_context
{
    const string CookieName = "test-cookie";
    const string CookiePath = "/api/v1";
    HttpListenerContext _listenerContext;
    string[] _setCookieHeaders;

    async Task Establish()
    {
        _listenerContext = await GetListenerContext();
        _requestContext = new HttpListenerRequestContext(_listenerContext, _serviceProvider);

        _requestContext.AppendCookie(CookieName, "original-value", new CookieOptions { Path = CookiePath });
    }

    void Because()
    {
        _requestContext.RemoveCookie(CookieName);
        _setCookieHeaders = _listenerContext.Response.Headers.GetValues("Set-Cookie") ?? [];
    }

    [Fact] void should_have_two_set_cookie_headers() => _setCookieHeaders.Length.ShouldEqual(2);
    [Fact] void should_have_removal_header_with_empty_value() => _setCookieHeaders[1].ShouldContain($"{CookieName}=;");
    [Fact] void should_have_removal_header_with_expires_in_past() => _setCookieHeaders[1].ShouldContain("Expires=");
    [Fact] void should_have_removal_header_with_root_path() => _setCookieHeaders[1].ShouldContain("Path=/");
}
