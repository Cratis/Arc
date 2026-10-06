// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Linq.Expressions;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_paged_queryable_that_cannot_translate_cast : given.a_query_pipeline
{
    readonly FullyQualifiedQueryName _queryName = "PagedQuery";
    readonly object[] _items = [new { id = 2 }, new { id = 3 }];
    IQueryProvider _provider;
    IQueryable<object> _window;
    IEnumerable<object> _intercepted;
    QueryResult _result;

    void Establish()
    {
        // Model the already-rendered page from a provider such as MongoDB LINQ 3: enumeration works,
        // but composing another provider-side operation (including Cast<object>) is unsupported.
        _provider = Substitute.For<IQueryProvider>();
        _provider.CreateQuery<object>(Arg.Any<Expression>()).Returns(_ => throw new NotSupportedException("Cast is not translatable"));
        _window = Substitute.For<IQueryable<object>>();
        _window.Provider.Returns(_provider);
        _window.Expression.Returns(_items.AsQueryable().Skip(1).Take(2).Expression);
        _window.GetEnumerator().Returns(_ => _items.AsEnumerable().GetEnumerator());
        ((IEnumerable)_window).GetEnumerator().Returns(_ => _items.GetEnumerator());
        _queryPerformer.ReadModelType.Returns(typeof(object));
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(QueryResult.Success(_correlationId));
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(_window));
        _queryRenderers.Render(_queryName, _window, _serviceProvider).Returns(new QueryRendererResult(5, _window));
        _readModelInterceptors.Intercept(typeof(object), Arg.Any<IEnumerable<object>>(), _serviceProvider)
            .Returns(callInfo =>
            {
                _intercepted = callInfo.ArgAt<IEnumerable<object>>(1);
                return Task.FromResult(_intercepted);
            });
    }

    async Task Because() => _result = await _pipeline.Perform(_queryName, [], new Paging(1, 2, true), Sorting.None, _serviceProvider);

    [Fact] void should_return_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_pass_the_materialized_page_to_interceptors() => _intercepted.ShouldContainOnly(_items);
    [Fact] void should_return_the_intercepted_items() => _result.Data.ShouldEqual(_intercepted);
    [Fact] void should_preserve_the_total_items() => _result.Paging.TotalItems.ShouldEqual(5);
    [Fact] void should_not_compose_a_provider_side_cast() => _provider.DidNotReceive().CreateQuery<object>(Arg.Any<Expression>());
}
