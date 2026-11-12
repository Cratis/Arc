// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_EndpointRouteHelper.when_getting_status_code;

public class and_the_caller_is_denied_and_the_failure_is_retryable : Specification
{
    HttpStatusCode _result;

    void Because() => _result = EndpointRouteHelper.GetStatusCode(isSuccess: false, isAuthorized: false, isValid: true, isRetryable: true);

    [Fact] void should_map_to_forbidden() => _result.ShouldEqual(HttpStatusCode.Forbidden);
}
