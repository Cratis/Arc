// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Queries.Filters;

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class and_availability_denies_before_authorization : given.a_query_filters
{
    IAuthorizationQueryFilter _authorizationFilter;
    IQueryFilter _ordinaryFilter;
    QueryResult _result;

    void Establish()
    {
        _authorizationFilter = Substitute.For<IAuthorizationQueryFilter>();
        _ordinaryFilter = Substitute.For<IQueryFilter>();
        var availabilityFilter = Substitute.For<IAvailabilityQueryFilter>();
        availabilityFilter.OnPerform(_queryContext).Returns(Task.FromResult(QueryResult.Unauthorized(_correlationId, AvailabilityReason.NotAvailable)));

        // Discovered last, and an authorization filter besides: availability must still be decided first.
        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([_ordinaryFilter, _authorizationFilter, availabilityFilter]), _activitySource);
    }

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_report_the_not_available_reason() => _result.AuthorizationFailureReason.ShouldEqual(AvailabilityReason.NotAvailable);
    [Fact] void should_not_evaluate_authorization() => _authorizationFilter.DidNotReceive().OnPerform(_queryContext);
    [Fact] void should_not_run_ordinary_filters() => _ordinaryFilter.DidNotReceive().OnPerform(_queryContext);
}
