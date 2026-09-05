// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

/// <summary>
/// An observable query result is a subject wrapper, not a read model instance. It must flow through the pipeline
/// untouched so the streaming transport can intercept each emission; handing the wrapper to the per-item read
/// model interceptor here would try to bind the subject to the read model type and throw at append time.
/// </summary>
public class with_observable_query_result : given.a_query_pipeline
{
    FullyQualifiedQueryName _queryName;
    QueryArguments _parameters;
    Paging _paging;
    Sorting _sorting;
    QueryResult _filterResult;
    ISubject<IEnumerable<object>> _subject;
    QueryRendererResult _rendererResult;
    QueryResult _result;

    void Establish()
    {
        _queryName = "TestQuery";
        _parameters = new();
        _paging = Paging.NotPaged;
        _sorting = Sorting.None;

        _filterResult = QueryResult.Success(_correlationId);
        _subject = new ReplaySubject<IEnumerable<object>>();
        _rendererResult = new QueryRendererResult(0, _subject);

        _queryPerformer.ReadModelType.Returns(typeof(object));
        _queryPerformer.Dependencies.Returns(new List<Type>());
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(_filterResult);
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(_subject));
        _queryRenderers.Render(_queryName, _subject, _serviceProvider).Returns(_rendererResult);
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, _parameters, _paging, _sorting, _serviceProvider);

    [Fact] void should_return_successful_result() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_capture_any_exception() => _result.ExceptionMessages.ShouldBeEmpty();
    [Fact] void should_return_the_subject_untouched() => _result.Data.ShouldEqual(_subject);
    [Fact] void should_not_invoke_the_read_model_interceptors() =>
        _readModelInterceptors.DidNotReceive().Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>());
}
