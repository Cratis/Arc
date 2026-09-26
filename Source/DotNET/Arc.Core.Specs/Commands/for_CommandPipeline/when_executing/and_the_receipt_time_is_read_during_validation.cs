// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_the_receipt_time_is_read_during_validation : given.a_command_pipeline_and_a_handler_for_command
{
    readonly DateTimeOffset _now = new(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
    CommandContext _context = null!;
    DateTimeOffset? _validatorReceipt;
    DateTimeOffset? _after;

    void Establish()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_now);
        _serviceProvider.GetService(typeof(TimeProvider)).Returns(clock);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
        {
            _context = call.Arg<CommandContext>();
            _validatorReceipt = ((IOperationContextAccessor)_context.ServiceProvider!.GetService(typeof(IOperationContextAccessor))!).ReceivedAt;
            return CommandResult.Success(_correlationId);
        });
        _serviceProvider.GetService(typeof(IOperationContextAccessor)).Returns(new OperationContextAccessor());
    }

    async Task Because()
    {
        await _commandPipeline.Validate(_command, _serviceProvider);
        _after = new OperationContextAccessor().ReceivedAt;
    }

    [Fact] void should_set_command_context_at_receipt() => _context.ReceivedAt.ShouldEqual(_now);
    [Fact] void should_expose_the_same_value_to_a_validator_in_the_command_scope() => _validatorReceipt.ShouldEqual(_now);
    [Fact] void should_not_leave_the_receipt_on_the_calling_flow() => _after.ShouldBeNull();
}
