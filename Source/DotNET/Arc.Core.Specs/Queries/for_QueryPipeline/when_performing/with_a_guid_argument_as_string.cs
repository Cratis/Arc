// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_guid_argument_as_string : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Guid _idValue;
    QueryContext _capturedContext;
    QueryResult _result;

    void Establish()
    {
        _queryName = "TestQuery";
        _idValue = Guid.NewGuid();
        _parameters = new() { { "id", _idValue.ToString() } };

        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformer.Parameters.Returns(new QueryParameters([new QueryParameter("id", typeof(Guid))]));
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

    [Fact] void should_coerce_the_argument_to_a_guid() => _capturedContext.Arguments["id"].ShouldBeOfExactType<Guid>();
    [Fact] void should_coerce_to_the_correct_guid_value() => _capturedContext.Arguments["id"].ShouldEqual(_idValue);
}
