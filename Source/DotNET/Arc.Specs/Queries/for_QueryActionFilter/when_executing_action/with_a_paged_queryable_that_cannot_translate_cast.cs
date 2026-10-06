// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing_action;

public class with_a_paged_queryable_that_cannot_translate_cast : given.a_query_action_filter
{
    readonly TestReadModel[] _items = [new("second"), new("third")];
    IQueryProvider _provider;
    IQueryable<TestReadModel> _window;
    IEnumerable<object> _intercepted;
    ActionExecutedContext _executedContext;
    QueryResult _result;

    void Establish()
    {
        // Model an already-rendered page whose provider supports enumeration but cannot translate Cast<object>.
        _provider = Substitute.For<IQueryProvider>();
        _provider.CreateQuery<object>(Arg.Any<Expression>()).Returns(_ => throw new NotSupportedException("Cast is not translatable"));
        _window = Substitute.For<IQueryable<TestReadModel>>();
        _window.Provider.Returns(_provider);
        _window.Expression.Returns(_items.AsQueryable().Skip(2).Take(2).Expression);
        _window.GetEnumerator().Returns(_ => _items.AsEnumerable().GetEnumerator());
        ((IEnumerable)_window).GetEnumerator().Returns(_ => _items.GetEnumerator());
        _httpContext.Request.QueryString = new QueryString("?page=1&pageSize=2");
        _queryRenderers.Render(Arg.Any<FullyQualifiedQueryName>(), _window, _httpContext.RequestServices)
            .Returns(new QueryRendererResult(5, _window));
        _readModelInterceptors.Intercept(typeof(TestReadModel), Arg.Any<IEnumerable<object>>(), _httpContext.RequestServices)
            .Returns(callInfo =>
            {
                _intercepted = callInfo.ArgAt<IEnumerable<object>>(1).ToArray();
                return Task.FromResult(_intercepted);
            });
        _executedContext = new ActionExecutedContext(_actionContext, [], null!)
        {
            Result = new ObjectResult(_window)
        };
    }

    async Task Because()
    {
        await _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(_executedContext));
        _result = (QueryResult)((ObjectResult)_executedContext.Result!).Value!;
    }

    [Fact] void should_return_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_http_ok() => _httpContext.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
    [Fact] void should_pass_the_page_to_interceptors() => _intercepted.ShouldContainOnly(_items);
    [Fact] void should_return_the_intercepted_items() => _result.Data.ShouldEqual(_intercepted);
    [Fact] void should_preserve_the_page() => _result.Paging.Page.ShouldEqual(1);
    [Fact] void should_preserve_the_page_size() => _result.Paging.Size.ShouldEqual(2);
    [Fact] void should_preserve_the_total_items() => _result.Paging.TotalItems.ShouldEqual(5);
    [Fact] void should_not_compose_a_provider_side_cast() => _provider.DidNotReceive().CreateQuery<object>(Arg.Any<Expression>());
}
