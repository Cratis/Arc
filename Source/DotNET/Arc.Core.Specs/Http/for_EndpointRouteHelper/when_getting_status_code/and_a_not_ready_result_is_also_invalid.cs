// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_EndpointRouteHelper.when_getting_status_code;

public class and_a_not_ready_result_is_also_invalid : Specification
{
    HttpStatusCode _result;

    void Because() => _result = EndpointRouteHelper.GetStatusCode(isSuccess: false, isAuthorized: true, isValid: false, isReady: false);

    [Fact] void should_map_to_bad_request() => _result.ShouldEqual(HttpStatusCode.BadRequest);
}
