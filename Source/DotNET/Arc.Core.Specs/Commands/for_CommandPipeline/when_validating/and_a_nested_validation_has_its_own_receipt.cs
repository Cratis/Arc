// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_validating;

public class and_a_nested_validation_has_its_own_receipt : given.a_command_pipeline_and_a_handler_for_command
{
    readonly DateTimeOffset _first = new(2026, 7, 8, 9, 10, 11, TimeSpan.Zero);
    DateTimeOffset _second;
    DateTimeOffset? _outerBefore;
    DateTimeOffset? _inner;
    DateTimeOffset? _outerAfter;
    DateTimeOffset? _after;
    int _depth;

    void Establish()
    {
        _second = _first.AddMinutes(1);
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_first, _second);
        _serviceProvider.GetService(typeof(TimeProvider)).Returns(clock);
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(async _ =>
        {
            var accessor = new OperationContextAccessor();
            if (Interlocked.Increment(ref _depth) == 1)
            {
                _outerBefore = accessor.ReceivedAt;
                await _commandPipeline.Validate(_command, _serviceProvider);
                _outerAfter = accessor.ReceivedAt;
            }
            else
            {
                _inner = accessor.ReceivedAt;
            }
            return CommandResult.Success(_correlationId);
        });
    }

    async Task Because()
    {
        await _commandPipeline.Validate(_command, _serviceProvider);
        _after = new OperationContextAccessor().ReceivedAt;
    }

    [Fact] void should_capture_the_outer_receipt() => _outerBefore.ShouldEqual(_first);
    [Fact] void should_capture_a_new_receipt_for_the_inner_validation() => _inner.ShouldEqual(_second);
    [Fact] void should_restore_the_outer_receipt() => _outerAfter.ShouldEqual(_first);
    [Fact] void should_clear_the_receipt_on_return() => _after.ShouldBeNull();
}
