// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing;

public class and_a_websocket_upgrade_with_multiple_proxy_keys_comes_from_another_origin : given.a_controller_query_request
{
    void Establish()
    {
        _options.Query.AllowedWebSocketOrigins = ["https://trusted.test"];
        _httpContext.Request.Headers.SecWebSocketKey = "first-proxy-key, dGhlIHNhbXBsZSBub25jZQ==";
        _webSocketFeature.IsWebSocketRequest.Returns(_ => _httpContext.Request.Headers.SecWebSocketKey.ToString() == "dGhlIHNhbXBsZSBub25jZQ==");
        _httpContext.Request.Headers.Host = "app.test";
        _httpContext.Request.Headers.Origin = "https://elsewhere.test";
    }

    Task Because() => _filter.OnActionExecutionAsync(_actionContext, _next);

    [Fact] void should_answer_forbidden() => ((StatusCodeResult)_actionContext.Result).StatusCode.ShouldEqual(StatusCodes.Status403Forbidden);
    [Fact] void should_not_run_the_action() => _next.DidNotReceive()();
    [Fact] void should_not_accept_the_connection() => _transportEvents.ShouldBeEmpty();
}
