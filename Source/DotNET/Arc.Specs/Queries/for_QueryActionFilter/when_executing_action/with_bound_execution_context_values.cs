// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Availability;
using Cratis.Arc.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing_action;

public class with_bound_execution_context_values : given.a_query_action_filter
{
    readonly DateTimeOffset _receipt = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    ServiceProvider _services;
    AvailabilityInvocation _invocation;
    QueryContext _context;

    void Establish()
    {
        var source = Substitute.For<IExecutionContextValueSource>();
        source.ReadForExecution(Arg.Any<AvailabilityInvocation>(), "selected", Arg.Any<CancellationToken>()).Returns(call =>
        {
            _invocation = call.Arg<AvailabilityInvocation>();
            return ContextValueRead.Present("found");
        });
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_receipt);
        _services = new ServiceCollection().AddSingleton<IContextValueSource>(source).AddSingleton(clock).BuildServiceProvider();
        _httpContext.RequestServices = _services;
        _actionContext.ActionArguments["count"] = 42;
        ((ControllerActionDescriptor)_actionContext.ActionDescriptor).ControllerTypeInfo = typeof(TestReadModel).GetTypeInfo();
        _queryContextManager.When(manager => manager.Set(Arg.Any<QueryContext>())).Do(call => _context = call.Arg<QueryContext>());
    }

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, async () =>
    {
        await _context.ProvidedValues.GetAsync("selected");
        return new ActionExecutedContext(_actionContext, [], null!) { Result = new StatusCodeResult(204) };
    });

    void Destroy() => _services.Dispose();

    [Fact] void should_pass_mvc_bound_arguments() => ((QueryArguments)_invocation.Arguments)["count"].ShouldEqual(42);
    [Fact] void should_use_the_controller_identity() => _invocation.Part.ShouldEqual(typeof(TestReadModel));
    [Fact] void should_pass_the_transport_receipt() => _invocation.ReceivedAt.ShouldEqual(_receipt);
    [Fact] void should_keep_the_request_service_scope() => _context.ServiceProvider.ShouldBeSame(_services);
}
