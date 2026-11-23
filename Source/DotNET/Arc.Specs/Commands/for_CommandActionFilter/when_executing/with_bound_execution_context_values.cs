// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class with_bound_execution_context_values : given.a_post_request_for_a_command_action
{
    readonly DateTimeOffset _receipt = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    ServiceProvider _services;
    AvailabilityInvocation _invocation;
    CommandContext _context;

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
        _actionContext.HttpContext.RequestServices = _services;
        _contextModifier.When(modifier => modifier.SetCurrent(Arg.Any<CommandContext>())).Do(call => _context = call.Arg<CommandContext>());
    }

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, async () =>
    {
        await _context.ProvidedValues.GetAsync("selected");
        return _executedContext;
    });

    void Destroy() => _services.Dispose();

    [Fact] void should_pass_the_mvc_bound_body() => _invocation.Arguments.ShouldBeSame(_command);
    [Fact] void should_use_the_body_type() => _invocation.Part.ShouldEqual(typeof(TheCommand));
    [Fact] void should_pass_the_mvc_receipt() => _invocation.ReceivedAt.ShouldEqual(_receipt);
}
