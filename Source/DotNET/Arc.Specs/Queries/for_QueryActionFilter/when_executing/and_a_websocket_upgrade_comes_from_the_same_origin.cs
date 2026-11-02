// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing;

public class and_a_websocket_upgrade_comes_from_the_same_origin : given.a_controller_query_request
{
    void Establish()
    {
        _options.Query.AllowedWebSocketOrigins.Add("https://trusted.test");
        _webSocketFeature.IsWebSocketRequest.Returns(true);
        _httpContext.Request.Headers.Host = "app.test";
        _httpContext.Request.Headers.Origin = "http://app.test";
    }

    Task Because() => _filter.OnActionExecutionAsync(_actionContext, _next);

    [Fact] void should_run_the_action() => _next.Received(1)();
    [Fact] void should_not_refuse_the_connection() => _actionContext.Result.ShouldBeNull();
}
