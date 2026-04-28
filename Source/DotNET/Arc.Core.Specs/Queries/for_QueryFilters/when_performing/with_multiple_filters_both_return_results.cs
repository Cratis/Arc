// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryFilters.when_performing;

public class with_multiple_filters_both_return_results : given.a_query_filters
{
    IQueryFilter _firstFilter;
    IQueryFilter _secondFilter;
    QueryResult _firstFilterResult;
    QueryResult _secondFilterResult;
    QueryResult _result;

    void Establish()
    {
        _firstFilter = Substitute.For<IQueryFilter>();
        _secondFilter = Substitute.For<IQueryFilter>();

        _firstFilterResult = new QueryResult
        {
            CorrelationId = _correlationId,
            ValidationResults = [new ValidationResult(ValidationResultSeverity.Error, "First validation error", [], "SomeProperty")],
            IsAuthorized = false
        };

        _secondFilterResult = new QueryResult
        {
            CorrelationId = _correlationId,
            ExceptionMessages = ["Second filter exception"],
            ExceptionStackTrace = "Stack trace from second filter"
        };

        _firstFilter.OnPerform(_queryContext).Returns(Task.FromResult(_firstFilterResult));
        _secondFilter.OnPerform(_queryContext).Returns(Task.FromResult(_secondFilterResult));

        _queryFilters = new QueryFilters(new KnownInstancesOf<IQueryFilter>([_firstFilter, _secondFilter]));
    }

    async Task Because() => _result = await _queryFilters.OnPerform(_queryContext);

    [Fact] void should_call_first_filter() => _firstFilter.Received(1).OnPerform(_queryContext);
    [Fact] void should_call_second_filter() => _secondFilter.Received(1).OnPerform(_queryContext);
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_merge_validation_results() => _result.ValidationResults.ShouldContain(r => r.Message == "First validation error");
    [Fact] void should_merge_exception_messages() => _result.ExceptionMessages.ShouldContain("Second filter exception");
    [Fact] void should_merge_exception_stack_trace() => _result.ExceptionStackTrace.ShouldContain("Stack trace from second filter");
    [Fact] void should_not_be_authorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_exceptions() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_not_be_success() => _result.IsSuccess.ShouldBeFalse();
}