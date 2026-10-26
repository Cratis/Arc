// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries.for_QueryActionFilter.given.Billing;
using Cratis.Arc.Slices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing_action;

public class for_a_controller : given.a_query_action_filter
{
    QueryContext _establishedContext;

    void Establish()
    {
        ((ControllerActionDescriptor)_actionContext.ActionDescriptor).ControllerTypeInfo = typeof(InvoicesController).GetTypeInfo();
        _queryContextManager
            .When(m => m.Set(Arg.Any<QueryContext>()))
            .Do(call => _establishedContext = call.Arg<QueryContext>());
        _queryRenderers.Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>(), Arg.Any<IServiceProvider>())
            .Returns(new QueryRendererResult(1, new TestReadModel("single")));
    }

    async Task Because() =>
        await _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(new ActionExecutedContext(_actionContext, [], null!)
        {
            Result = new ObjectResult(new TestReadModel("single"))
        }));

    [Fact] void should_resolve_the_slice_location_from_the_controller() => _establishedContext.SliceLocation.ShouldEqual(SliceLocation.For(typeof(InvoicesController)));
    [Fact] void should_end_the_slice_location_at_the_controller_namespace() => _establishedContext.SliceLocation.Slice.Value.ShouldEqual("Billing");
}
