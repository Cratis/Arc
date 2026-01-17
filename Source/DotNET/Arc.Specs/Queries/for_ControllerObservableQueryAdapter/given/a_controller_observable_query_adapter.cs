// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter.given;

public class a_controller_observable_query_adapter : Specification
{
    protected IQueryContextManager _queryContextManager;
    protected IOptions<JsonOptions> _jsonOptions;
    protected ILogger<ControllerObservableQueryAdapter> _logger;
    protected ControllerObservableQueryAdapter _adapter;
    protected ActionExecutingContext _actionContext;
    protected HttpContext _httpContext;

    void Establish()
    {
        _queryContextManager = Substitute.For<IQueryContextManager>();
        _jsonOptions = Substitute.For<IOptions<JsonOptions>>();
        _jsonOptions.Value.Returns(new JsonOptions());
        _logger = Substitute.For<ILogger<ControllerObservableQueryAdapter>>();

        _httpContext = new DefaultHttpContext
        {
            RequestServices = Substitute.For<IServiceProvider>()
        };

        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerName = "TestController",
            ActionName = "TestAction"
        };

        var actionContext = new ActionContext(_httpContext, new Microsoft.AspNetCore.Routing.RouteData(), actionDescriptor, new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        _actionContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            null!);

        _adapter = new ControllerObservableQueryAdapter(_queryContextManager, _jsonOptions, _logger);
    }

    protected record TestData(string Value);
}
