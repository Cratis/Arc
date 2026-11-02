// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing;

public class and_a_websocket_upgrade_comes_from_a_different_port : given.a_controller_query_request
{
    void Establish()
    {
        _options.Query.AllowedWebSocketOrigins.Add("https://trusted.test");
        _webSocketFeature.IsWebSocketRequest.Returns(true);
        _httpContext.Request.Headers.Host = "app.test";
        _httpContext.Request.Headers.Origin = "http://app.test:81";
    }

    Task Because() => _filter.OnActionExecutionAsync(_actionContext, _next);

    [Fact] void should_answer_forbidden() => ((StatusCodeResult)_actionContext.Result).StatusCode.ShouldEqual(StatusCodes.Status403Forbidden);
    [Fact] void should_not_run_the_action() => _next.DidNotReceive()();
    [Fact] void should_not_accept_the_connection() => _transportEvents.ShouldBeEmpty();
}
