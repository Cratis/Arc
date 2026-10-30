// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing_action;

public class and_sse_tenant_membership_is_denied : given.a_controller_stream_with_tenant_membership
{
    Task Because() => RunDeniedStream(false);

    [Fact] void should_respond_forbidden() => _httpContext.Response.StatusCode.ShouldEqual(StatusCodes.Status403Forbidden);
    [Fact] void should_not_write_a_stream() => ResponseBody().ShouldEqual(string.Empty);
    [Fact] void should_not_set_sse_content_type() => _httpContext.Response.ContentType.ShouldBeNull();
    [Fact] void should_return_an_unauthorized_result() => ((QueryResult)((ObjectResult)(_actionContext.Result ?? _executed.Result)!).Value!).IsAuthorized.ShouldBeFalse();
}
