// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_invalid_collection_element : given.a_query_pipeline
{
    QueryResult _result;

    void Establish()
    {
        var queryName = new FullyQualifiedQueryName("QueryWithInvalidCollection");
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(QueryResult.Success(_correlationId));
        _queryPerformer.Perform(Arg.Any<QueryContext>())
            .Returns(ValueTask.FromException<object?>(new InvalidCollectionQueryArgument(typeof(int[]), "invalid")));
    }

    async Task Because() => _result = await _pipeline.Perform(
        "QueryWithInvalidCollection", new QueryArguments(), Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_fail_validation() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_report_a_server_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_report_malformed_request() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
}
