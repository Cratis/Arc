// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Specs.Fakes;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_chronicle_dependency_that_is_not_configured : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _result;

    void Establish()
    {
        _queryName = "AuthorsQuery";
        _parameters = [];
        _paging = Paging.NotPaged;
        _sorting = Sorting.None;

        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });
        _queryPerformer.Dependencies.Returns([typeof(FakeEventLog)]);
        _serviceProvider.GetService(typeof(FakeEventLog)).Returns(_ =>
            throw new InvalidOperationException(
                "Unable to resolve service for type 'Cratis.Chronicle.Specs.Fakes.FakeEventStoreName' " +
                "while attempting to activate 'Cratis.Chronicle.Specs.Fakes.FakeEventLog'."));
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting, _serviceProvider);

    [Fact] void should_return_unsuccessful_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_hint_that_chronicle_is_not_configured() => _result.ExceptionMessages.Any(_ => _.Contains("WithChronicle")).ShouldBeTrue();
    [Fact] void should_not_call_query_performer() => _queryPerformer.DidNotReceiveWithAnyArgs().Perform(Arg.Any<QueryContext>());
}
