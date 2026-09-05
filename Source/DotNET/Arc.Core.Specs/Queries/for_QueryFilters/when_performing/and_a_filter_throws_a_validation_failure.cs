// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class and_a_filter_throws_a_validation_failure : given.a_query_filters
{
    IQueryFilter _filter;
    QueryResult _result;

    void Establish()
    {
        _filter = Substitute.For<IQueryFilter>();

        // A filter whose dependency cannot be resolved without client-provided input throws an IValidationFailure.
        // It must become a 400, not a 500.
        _filter.OnPerform(_queryContext).Returns<Task<QueryResult>>(_ => throw new TheValidationFailure());

        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([_filter]), _activitySource);
    }

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_carry_any_exception_detail() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_surface_the_validation_message() => _result.ValidationResults.ShouldContain(vr => vr.Message == "missing identifier");
    [Fact] void should_map_to_bad_request_not_internal_server_error() => EndpointRouteHelper.GetStatusCode(_result.IsSuccess, _result.IsAuthorized, _result.IsValid).ShouldEqual(HttpStatusCode.BadRequest);

    class TheValidationFailure : Exception, IValidationFailure
    {
        public ValidationResult ValidationResult { get; } = ValidationResult.Error("missing identifier");
    }
}
