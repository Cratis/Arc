// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.Filters;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class and_authorization_denies_a_caller_whose_query_is_also_invalid : given.a_query_filters
{
    RecordingValidationFilter _validationFilter;
    QueryResult _result;

    void Establish()
    {
        var queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();
        var queryPerformer = Substitute.For<IQueryPerformer>();
        queryPerformer.IsAuthorized(_queryContext).Returns(false);
        queryPerformerProviders.TryGetPerformersFor(_queryContext.Name, out var _).Returns(callInfo =>
        {
            callInfo[1] = queryPerformer;
            return true;
        });
        var authorizationFilter = new AuthorizationFilter(queryPerformerProviders);

        _validationFilter = new RecordingValidationFilter();

        // The validation filter is registered first on purpose: authorization must still be decided first (by order),
        // so a forbidden caller whose query is also invalid gets a clean 403 and the validation filter never runs.
        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([_validationFilter, authorizationFilter]), _activitySource);
    }

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_map_to_forbidden_not_bad_request() => EndpointRouteHelper.GetStatusCode(_result.IsSuccess, _result.IsAuthorized, _result.IsValid).ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_run_the_validation_filter() => _validationFilter.WasCalled.ShouldBeFalse();
    [Fact] void should_not_leak_any_validation_detail() => _result.ValidationResults.ShouldBeEmpty();

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
