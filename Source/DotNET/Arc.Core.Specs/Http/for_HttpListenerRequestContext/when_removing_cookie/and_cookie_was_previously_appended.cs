// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_removing_cookie;

public class and_cookie_was_previously_appended : given.an_http_listener_request_context
{
    const string CookieName = "test-cookie";
    HttpListenerContext _listenerContext;
    string[] _setCookieHeaders;

    async Task Establish()
    {
        _listenerContext = await GetListenerContext();
        _requestContext = new HttpListenerRequestContext(_listenerContext, _serviceProvider);

        // First, append a cookie using the public method
        _requestContext.AppendCookie(CookieName, "original-value", new CookieOptions());
    }

    void Because()
    {
        _requestContext.RemoveCookie(CookieName);
        _setCookieHeaders = _listenerContext.Response.Headers.GetValues("Set-Cookie") ?? [];
    }

    [Fact] void should_have_two_set_cookie_headers() => _setCookieHeaders.Length.ShouldEqual(2);
    [Fact] void should_have_removal_header_with_empty_value() => _setCookieHeaders[1].ShouldContain($"{CookieName}=;");
    [Fact] void should_have_removal_header_with_expires_in_past() => _setCookieHeaders[1].ShouldContain("Expires=");
}
