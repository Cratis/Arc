// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_an_already_typed_concept_argument : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    TestId _id;
    QueryContext _capturedContext;
    QueryResult _result;

    void Establish()
    {
        _queryName = "TestQuery";
        _id = new TestId(Guid.NewGuid());
        _parameters = new() { { "id", _id } };

        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformer.Parameters.Returns(new QueryParameters([new QueryParameter("id", typeof(TestId))]));
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Do<QueryContext>(ctx => _capturedContext = ctx)).Returns(QueryResult.Success(_correlationId));
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(new { name = "Test" }));
        _queryRenderers.Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>(), Arg.Any<IServiceProvider>())
            .Returns(new QueryRendererResult(1, new { name = "Test" }));
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, Paging.NotPaged, Sorting.None, _serviceProvider);

    [Fact] void should_pass_the_same_instance_through_untouched() => ReferenceEquals(_capturedContext.Arguments["id"], _id).ShouldBeTrue();

    public record TestId(Guid Value) : ConceptAs<Guid>(Value);
}
