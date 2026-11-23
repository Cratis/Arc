// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryActionFilter.given;

public class a_query_with_injected_parameters : a_query_action_filter
{
    protected readonly QueryInput _input = new("caller-bound");
    protected QueryContext _context;
    protected ActionExecutionDelegate _next;
    ServiceProvider _services;

    void Establish()
    {
        _services = new ServiceCollection().AddSingleton(new QueryInput("registered"))
            .AddMvcCore().Services.BuildServiceProvider();
        _httpContext.RequestServices = _services;
        var descriptor = (ControllerActionDescriptor)_actionContext.ActionDescriptor;
        descriptor.ControllerTypeInfo = typeof(QueriesController).GetTypeInfo();
        descriptor.Parameters = typeof(QueriesController).GetMethod(nameof(QueriesController.Get))!.GetParameters()
            .Select(parameter => (ParameterDescriptor)new ControllerParameterDescriptor
            {
                Name = parameter.Name!,
                ParameterType = parameter.ParameterType,
                ParameterInfo = parameter,
                BindingInfo = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes())
            }).ToList();
        descriptor.Parameters.Single(parameter => parameter.Name == "keyed").BindingInfo = null;
        descriptor.Parameters.Single(parameter => parameter.Name == "renderer").BindingInfo = null;
        descriptor.Parameters.Add(new ParameterDescriptor
        {
            Name = "special",
            ParameterType = typeof(CancellationToken)
        });
        _actionContext.ActionArguments["count"] = 42;
        _actionContext.ActionArguments["id"] = "invoice-42";
        _actionContext.ActionArguments["renderer"] = _queryRenderers;
        _actionContext.ActionArguments["keyed"] = _readModelInterceptors;
        _actionContext.ActionArguments["cancellationToken"] = _httpContext.RequestAborted;
        _actionContext.ActionArguments["input"] = _input;
        _actionContext.ActionArguments["special"] = _httpContext.RequestAborted;
        _queryContextManager.When(manager => manager.Set(Arg.Any<QueryContext>())).Do(call => _context = call.Arg<QueryContext>());
        _next = () => Task.FromResult(new ActionExecutedContext(_actionContext, [], null!) { Result = new StatusCodeResult(204) });
    }

    void Destroy() => _services.Dispose();

    public record QueryInput(string Value);

    public class QueriesController : ControllerBase
    {
        public int Get(
            [FromQuery] int count,
            [FromRoute] string id,
            [FromServices] IQueryRenderers renderer,
            [FromKeyedServices("query")] IReadModelInterceptors keyed,
            QueryInput input,
            CancellationToken cancellationToken) => count;
    }
}
