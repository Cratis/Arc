// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.Filters.for_AvailabilityFilter;

public class when_the_gate_accepts_an_invocation : given.a_query_filter_with_a_gate
{
    IAvailabilityExecutionGate _executionGate;
    AvailabilityInvocation _invocation;
    readonly DateTimeOffset _receipt = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

    void Establish()
    {
        _executionGate = Substitute.For<IAvailabilityExecutionGate>();
        _executionGate.IsGated(typeof(GatedReadModel)).Returns(true);
        _executionGate.Decide(Arg.Any<AvailabilityInvocation>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _invocation = call.Arg<AvailabilityInvocation>();
                return AvailabilityDecision.Available;
            });
        _performer.ReadModelType.Returns(typeof(string));
        _services.Dispose();
        _services = new ServiceCollection().AddSingleton<IAvailabilityGate>(_executionGate).BuildServiceProvider();
        _context = _context with { ServiceProvider = _services, Arguments = new QueryArguments { ["vesselId"] = 42 }, ReceivedAt = _receipt };
    }

    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_pass_the_bound_arguments() => _invocation.Arguments.ShouldBeSame(_context.Arguments);
    [Fact] void should_keep_gating_on_the_performer_type() => _invocation.Part.ShouldEqual(typeof(GatedReadModel));
    [Fact] void should_not_select_the_read_model_type() => _executionGate.DidNotReceive().IsGated(typeof(string));
    [Fact] void should_pass_the_receipt_unchanged() => _invocation.ReceivedAt.ShouldEqual(_receipt);
    [Fact] void should_continue() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_use_the_metadata_overload() => _executionGate.DidNotReceive().Decide(Arg.Any<Type>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>());
}
