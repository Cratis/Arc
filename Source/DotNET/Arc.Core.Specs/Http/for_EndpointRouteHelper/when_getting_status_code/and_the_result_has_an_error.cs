// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Http.for_EndpointRouteHelper.when_getting_status_code;

public class and_the_result_has_an_error : Specification
{
    HttpStatusCode _result;

    void Because() => _result = EndpointRouteHelper.GetStatusCode(isSuccess: false, isAuthorized: true, isValid: true, isReady: true);

    [Fact] void should_map_to_internal_server_error() => _result.ShouldEqual(HttpStatusCode.InternalServerError);
}
