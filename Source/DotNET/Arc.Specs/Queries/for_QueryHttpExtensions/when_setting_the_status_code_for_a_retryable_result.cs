// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Queries.for_QueryHttpExtensions;

public class when_setting_the_status_code_for_a_retryable_result : Specification
{
    int _retryable;
    int _notAvailable;

    void Because()
    {
        var retryable = new DefaultHttpContext();
        retryable.Response.SetResponseStatusCode(QueryResult.Retryable(CorrelationId.New(), "retry"));
        _retryable = retryable.Response.StatusCode;

        var notAvailable = new DefaultHttpContext();
        notAvailable.Response.SetResponseStatusCode(QueryResult.Unauthorized(CorrelationId.New(), AvailabilityReason.NotAvailable));
        _notAvailable = notAvailable.Response.StatusCode;
    }

    [Fact] void should_report_service_unavailable() => _retryable.ShouldEqual(StatusCodes.Status503ServiceUnavailable);
    [Fact] void should_report_forbidden_for_a_part_that_is_not_available() => _notAvailable.ShouldEqual(StatusCodes.Status403Forbidden);
}
