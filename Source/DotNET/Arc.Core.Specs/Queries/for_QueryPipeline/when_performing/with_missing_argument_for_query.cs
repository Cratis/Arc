// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_missing_argument_for_query : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _filterResult;
    MissingArgumentForQuery _exception;
    QueryResult _result;

    void Establish()
    {
        _queryName = "QueryWithMissingArgument";
        _parameters = new();
        _paging = Paging.NotPaged;
        _sorting = Sorting.None;

        _filterResult = QueryResult.Success(_correlationId);
        _exception = new MissingArgumentForQuery("eventSourceId", typeof(Guid), _queryName);

        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_filterResult);
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromException<object?>(_exception));
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting, _serviceProvider);

    [Fact] void should_return_unsuccessful_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_have_exceptions() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_have_one_validation_result() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_have_validation_result_with_error_severity() => _result.ValidationResults.First().Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_have_validation_result_with_parameter_name_as_member() => _result.ValidationResults.First().Members.ShouldContain("eventSourceId");
    [Fact] void should_have_validation_result_with_exception_message() => _result.ValidationResults.First().Message.ShouldEqual(_exception.Message);
    [Fact] void should_have_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
