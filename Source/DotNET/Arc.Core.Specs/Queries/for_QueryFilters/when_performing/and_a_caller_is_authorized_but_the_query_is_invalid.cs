// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.Filters;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class and_a_caller_is_authorized_but_the_query_is_invalid : given.a_query_filters
{
    RecordingValidationFilter _validationFilter;
    QueryResult _result;

    void Establish()
    {
        var queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();
        var queryPerformer = Substitute.For<IQueryPerformer>();
        queryPerformer.IsAuthorized(_queryContext).Returns(true);
        queryPerformerProviders.TryGetPerformersFor(_queryContext.Name, out var _).Returns(callInfo =>
        {
            callInfo[1] = queryPerformer;
            return true;
        });
        var authorizationFilter = new AuthorizationFilter(queryPerformerProviders);

        _validationFilter = new RecordingValidationFilter();

        // Authorization is decided first (by order) and passes, so validation still runs and the invalid query
        // is reported as a 400 — ordering must not suppress validation for an authorized caller.
        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([_validationFilter, authorizationFilter]), _activitySource);
    }

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_be_authorized() => _result.IsAuthorized.ShouldBeTrue();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_run_the_validation_filter() => _validationFilter.WasCalled.ShouldBeTrue();
    [Fact] void should_map_to_bad_request() => EndpointRouteHelper.GetStatusCode(_result.IsSuccess, _result.IsAuthorized, _result.IsValid).ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_surface_the_validation_message() => _result.ValidationResults.ShouldContain(vr => vr.Message == "name is required");

    class RecordingValidationFilter : IQueryFilter
    {
        public bool WasCalled { get; private set; }

        public Task<QueryResult> OnPerform(QueryContext context)
        {
            WasCalled = true;
            return Task.FromResult(new QueryResult
            {
                CorrelationId = context.CorrelationId,
                ValidationResults = [ValidationResult.Error("name is required")]
            });
        }
    }
}
