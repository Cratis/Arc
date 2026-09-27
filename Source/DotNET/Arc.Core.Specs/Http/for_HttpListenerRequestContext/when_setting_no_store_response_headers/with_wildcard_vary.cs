// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_setting_no_store_response_headers;

public class with_wildcard_vary : given.an_http_listener_request_context
{
    HttpListenerContext _listenerContext;

    async Task Establish()
    {
        _listenerContext = await GetListenerContext();
        _requestContext = new HttpListenerRequestContext(_listenerContext, _serviceProvider);
        _listenerContext.Response.Headers["Vary"] = "*";
    }

    void Because() => _requestContext.SetNoStoreResponseHeaders();

    [Fact] void should_keep_the_wildcard_on_the_response() => _listenerContext.Response.Headers["Vary"].ShouldEqual("*");
}
