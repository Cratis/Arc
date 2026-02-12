// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_removing_cookie;

public class without_existing_cookie : given.an_http_listener_request_context
{
    const string CookieName = "test-cookie";
    HttpListenerContext _listenerContext;
    string[] _setCookieHeaders;

    async Task Establish()
    {
        _listenerContext = await GetListenerContext();
        _requestContext = new HttpListenerRequestContext(_listenerContext, _serviceProvider);
    }

    void Because()
    {
        _requestContext.RemoveCookie(CookieName);
        _setCookieHeaders = _listenerContext.Response.Headers.GetValues("Set-Cookie") ?? [];
    }

    [Fact] void should_add_set_cookie_header() => _setCookieHeaders.Length.ShouldEqual(1);
    [Fact] void should_have_empty_value() => _setCookieHeaders[0].ShouldContain($"{CookieName}=;");
    [Fact] void should_have_expired_date() => _setCookieHeaders[0].ShouldContain("Expires=");
}
