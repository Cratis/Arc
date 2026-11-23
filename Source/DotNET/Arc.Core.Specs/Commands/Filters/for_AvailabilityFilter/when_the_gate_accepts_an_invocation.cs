// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.Filters.for_AvailabilityFilter;

public class when_the_gate_accepts_an_invocation : Specification
{
    readonly DateTimeOffset _receipt = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    IAvailabilityExecutionGate _gate;
    ServiceProvider _services;
    CommandContext _context;
    AvailabilityInvocation _invocation;
    CommandResult _result;

    void Establish()
    {
        _gate = Substitute.For<IAvailabilityExecutionGate>();
        _gate.IsGated(typeof(Command)).Returns(true);
        _gate.Decide(Arg.Any<AvailabilityInvocation>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _invocation = call.Arg<AvailabilityInvocation>();
                return AvailabilityDecision.Available;
            });
        _services = new ServiceCollection().AddSingleton<IAvailabilityGate>(_gate).BuildServiceProvider();
        _context = new CommandContext(CorrelationId.New(), typeof(Command), new Command("vessel-42"), [], new CommandContextValues(), null, null, _services)
        {
            ReceivedAt = _receipt
        };
    }

    async Task Because() => _result = await new AvailabilityFilter().OnExecution(_context);

    void Destroy() => _services.Dispose();

    [Fact] void should_pass_the_command_instance() => _invocation.Arguments.ShouldBeSame(_context.Command);
    [Fact] void should_keep_the_command_identity() => _invocation.Part.ShouldEqual(typeof(Command));
    [Fact] void should_pass_the_pinned_receipt() => _invocation.ReceivedAt.ShouldEqual(_receipt);
    [Fact] void should_continue() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_use_the_metadata_overload() => _gate.DidNotReceive().Decide(Arg.Any<Type>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());

    public record Command(string VesselId);
}
