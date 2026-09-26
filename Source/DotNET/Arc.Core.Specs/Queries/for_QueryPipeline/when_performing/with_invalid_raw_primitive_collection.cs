// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_invalid_raw_primitive_collection : given.a_query_pipeline
{
    QueryResult _result;

    void Establish()
    {
        var queryName = new FullyQualifiedQueryName("Rates.ByIds");
        _queryPerformer.FullyQualifiedName.Returns(queryName);
        _queryPerformer.Parameters.Returns(new QueryParameters([new QueryParameter("ids", typeof(int[]))]));
        _queryPerformerProviders.TryGetPerformersFor(queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });
    }

    async Task Because() => _result = await _pipeline.Perform(
        "Rates.ByIds", new QueryArguments { ["ids"] = "1,bad" }, Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_fail_validation_for_ids() => _result.ValidationResults.Single().Members.ShouldContainOnly("ids");
    [Fact] void should_report_error_severity() => _result.ValidationResults.Single().Severity.ShouldEqual(ValidationResultSeverity.Error);
    [Fact] void should_not_report_a_server_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_not_invoke_the_performer() => _queryPerformer.DidNotReceive().Perform(Arg.Any<QueryContext>());
}
