// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_validation_checks_execution_availability : given.a_command_pipeline_and_a_handler_for_command
{
    readonly DateTimeOffset _receipt = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    AvailabilityInvocation _invocation;
    CommandResult _result;

    void Establish()
    {
        var gate = Substitute.For<IAvailabilityExecutionGate>();
        gate.IsGated(_command.GetType()).Returns(true);
        gate.Decide(Arg.Any<AvailabilityInvocation>(), _serviceProvider, Arg.Any<CancellationToken>()).Returns(call =>
        {
            _invocation = call.Arg<AvailabilityInvocation>();
            return new AvailabilityDecision(AvailabilityVerdict.Unknown, []);
        });
        _serviceProvider.GetService(typeof(IAvailabilityGate)).Returns(gate);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_receipt);
        _serviceProvider.GetService(typeof(TimeProvider)).Returns(clock);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call => new Filters.AvailabilityFilter().OnExecution(call.Arg<CommandContext>()));
    }

    async Task Because() => _result = await _commandPipeline.Validate(_command, _serviceProvider);

    [Fact] void should_pass_the_command_to_the_gate() => _invocation.Arguments.ShouldBeSame(_command);
    [Fact] void should_use_the_validation_operation_receipt() => _invocation.ReceivedAt.ShouldEqual(_receipt);
    [Fact] void should_fail_closed() => _result.IsAuthorized.ShouldBeFalse();
}
