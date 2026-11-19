// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_QueryResult;

public class when_creating_an_unauthorized_result_with_a_reason : Specification
{
    QueryResult _result;
    QueryResult _merged;

    void Because()
    {
        _result = QueryResult.Unauthorized(CorrelationId.New(), AvailabilityReason.NotAvailable);
        _merged = QueryResult.Success(CorrelationId.New());
        _merged.MergeWith(_result);
    }

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_carry_the_reason() => _result.AuthorizationFailureReason.ShouldEqual(AvailabilityReason.NotAvailable);
    [Fact] void should_carry_the_reason_when_merged() => _merged.AuthorizationFailureReason.ShouldEqual(AvailabilityReason.NotAvailable);
}
