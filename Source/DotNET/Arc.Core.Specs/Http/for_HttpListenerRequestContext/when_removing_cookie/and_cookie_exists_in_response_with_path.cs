// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_removing_cookie;

public class and_cookie_exists_in_response_with_path : given.an_http_listener_request_context
{
    const string CookieName = "test-cookie";
    const string CookiePath = "/api/v1";
    HttpListenerContext _listenerContext;
    Cookie _cookieAfterRemove;

    async Task Establish()
    {
        _listenerContext = await GetListenerContext();
        _requestContext = new HttpListenerRequestContext(_listenerContext, _serviceProvider);

        _requestContext.AppendCookie(CookieName, "original-value", new CookieOptions { Path = CookiePath });
    }

    void Because()
    {
        _requestContext.RemoveCookie(CookieName);
        _cookieAfterRemove = _listenerContext.Response.Cookies[CookieName]!;
    }

    [Fact] void should_have_only_one_cookie_in_response() => _listenerContext.Response.Cookies.Count.ShouldEqual(1);
    [Fact] void should_have_empty_value() => _cookieAfterRemove.Value.ShouldEqual(string.Empty);
    [Fact] void should_have_expired_date() => _cookieAfterRemove.Expires.ShouldBeLessThan(DateTime.Now);
    [Fact] void should_preserve_the_path() => _cookieAfterRemove.Path.ShouldEqual(CookiePath);
}
