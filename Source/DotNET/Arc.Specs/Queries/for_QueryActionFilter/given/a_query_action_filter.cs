// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Queries.for_QueryActionFilter.given;

public class a_query_action_filter : Specification
{
    protected IQueryContextManager _queryContextManager;
    protected IQueryRenderers _queryRenderers;
    protected IReadModelInterceptors _readModelInterceptors;
    protected ControllerObservableQueryAdapter _controllerAdapter;
    protected ILogger<QueryActionFilter> _logger;
    protected QueryActionFilter _filter;
    protected HttpContext _httpContext;
    protected ActionExecutingContext _actionContext;

    void Establish()
    {
        _queryContextManager = Substitute.For<IQueryContextManager>();
        _queryRenderers = Substitute.For<IQueryRenderers>();
        _readModelInterceptors = Substitute.For<IReadModelInterceptors>();
        _readModelInterceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(callInfo => Task.FromResult(callInfo.ArgAt<IEnumerable<object>>(1)));
        _logger = Substitute.For<ILogger<QueryActionFilter>>();

        var queryContextManager = Substitute.For<IQueryContextManager>();
        _controllerAdapter = new ControllerObservableQueryAdapter(queryContextManager, Substitute.For<ILogger<ControllerObservableQueryAdapter>>());

        _httpContext = new DefaultHttpContext
        {
            RequestServices = Substitute.For<IServiceProvider>()
        };
        _httpContext.Request.Method = HttpMethods.Get;

        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerName = "TestController",
            ActionName = "TestAction",
            DisplayName = "[TestApp].[TestQuery]"
        };

        var actionContext = new ActionContext(
            _httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            actionDescriptor,
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());

        _actionContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            null!);

        _filter = new QueryActionFilter(
            _queryContextManager,
            _queryRenderers,
            _readModelInterceptors,
            _controllerAdapter,
            _logger);
    }

    protected record TestReadModel(string Value);
}
