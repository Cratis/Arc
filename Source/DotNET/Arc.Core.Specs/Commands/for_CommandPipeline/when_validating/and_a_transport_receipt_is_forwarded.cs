// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_a_transport_receipt_is_forwarded : given.a_command_pipeline_and_a_handler_for_command
{
    readonly DateTimeOffset _received = new(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
    DateTimeOffset? _contextReceipt;
    DateTimeOffset? _after;

    void Establish()
    {
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_received, _received.AddMinutes(1));
        _serviceProvider.GetService(typeof(TimeProvider)).Returns(clock);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
        {
            _contextReceipt = call.Arg<CommandContext>().ReceivedAt;
            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because()
    {
        using (OperationContextScope.Begin(_serviceProvider))
        {
            using var dispatch = OperationContextScope.ForwardTransportReceipt();
            await _commandPipeline.Validate(_command, _serviceProvider);
        }
        _after = new OperationContextAccessor().ReceivedAt;
    }

    [Fact] void should_keep_the_receipt_captured_before_the_public_entry() => _contextReceipt.ShouldEqual(_received);
    [Fact] void should_restore_the_receipt_after_dispatch() => _after.ShouldBeNull();
}
