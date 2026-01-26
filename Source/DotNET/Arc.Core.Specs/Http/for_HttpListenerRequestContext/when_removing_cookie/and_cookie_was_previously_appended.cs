// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_removing_cookie;

public class and_cookie_was_previously_appended : given.an_http_listener_request_context
{
    const string CookieName = "test-cookie";
    HttpListenerContext _listenerContext;
    int _cookieCountAfterRemove;
    string _cookieValueAfterRemove;

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
        _cookieCountAfterRemove = _listenerContext.Response.Cookies.Count;
        _cookieValueAfterRemove = _listenerContext.Response.Cookies[CookieName]!.Value;
    }

    [Fact] void should_have_only_one_cookie_in_response() => _cookieCountAfterRemove.ShouldEqual(1);
    [Fact] void should_have_empty_value() => _cookieValueAfterRemove.ShouldEqual(string.Empty);
    [Fact] void should_have_expired_cookie() => _listenerContext.Response.Cookies[CookieName]!.Expires.ShouldBeLessThan(DateTime.Now);
}
