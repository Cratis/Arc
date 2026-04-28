// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class with_no_filters : given.a_query_filters
{
    QueryResult _result;

    void Establish() => _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([]));

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_return_success_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
    [Fact] void should_have_no_exceptions() => _result.HasExceptions.ShouldBeFalse();
}